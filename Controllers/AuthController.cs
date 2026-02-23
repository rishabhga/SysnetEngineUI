using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Services;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using static ManageEngineWebApp.Datacontext.RoleHelper;

namespace ManageEngineWebApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly string _baseUrl;
        private readonly string apiBaseUrl;
        private readonly IConfiguration _configuration;
        private readonly PermissionDiscoveryService _permissionDiscovery;

        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IConfiguration configuration, PermissionDiscoveryService permissionDiscovery, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _permissionDiscovery = permissionDiscovery;
            _httpClientFactory = httpClientFactory;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
            apiBaseUrl = $"{_baseUrl}/api/auth";
        }

        private HttpClient GetClient()
        {
            return _httpClientFactory.CreateClient("ManageEngineApi");
        }

        [HttpGet]
        [AllowAnonymous]
        [DynamicPermission("Auth.Register", "Register New User")]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            using var client = GetClient();
            var registerPayload = new
            {
                model.Username,
                model.Email,
                model.Password,
                model.ConfirmPassword
            };
            var json = JsonConvert.SerializeObject(registerPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{apiBaseUrl}/register", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["msg"] = $"Registration failed: {errorContent}";
                return View(model);
            }

            // Dynamic check: top-level admin can assign roles at registration
            if (RoleHelper.IsTopLevelAdmin(HttpContext) && !string.IsNullOrEmpty(model.Role))
            {
                var roleAssigned = await RoleHelper.AssignRoleAsync(model.Username, model.Role, model.CompanyId);
                if (roleAssigned)
                {
                    TempData["msg"] = $"User registered successfully as {model.Role}.";
                }
                else
                {
                    TempData["msg"] = "User registered but role assignment failed.";
                }
            }
            else
            {
                TempData["msg"] = "Registration successful. Please wait for role assignment.";
            }

            if (RoleHelper.IsTopLevelAdmin(HttpContext))
            {
                return RedirectToAction("ManageRoles");
            }
            return RedirectToAction("Login");
        }
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client, NoStore = false)]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto model)
        {
            using var client = GetClient();
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{apiBaseUrl}/login", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                errorMsg = errorMsg.Replace("\"", "").Trim();

                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    TempData["msg"] = "Server error. Please contact your administrator.";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["msg"] = "Invalid username or password.";
                }
                else if (string.IsNullOrEmpty(errorMsg) || errorMsg.Length > 150)
                {
                    TempData["msg"] = "Login failed. Please try again.";
                }
                else
                {
                    TempData["msg"] = errorMsg;
                }
                return View(model);
            }
            try
            {
                var roleResponse = await RoleHelper.GetUserRoleFromApiAsync(model.Username);

                if (roleResponse.Result == null)
                {
                    TempData["msg"] = roleResponse.Error ?? "Failed to retrieve user roles. API may be offline or returning errors.";
                    return View(model);
                }

                var roleData = roleResponse.Result;

                if (roleData.Roles == null || !roleData.Roles.Any())
                {
                    TempData["msg"] = "Your account has no role assigned. Contact your administrator.";
                    return View(model);
                }

                // Primary role = first role from API (already sorted by hierarchy level)
                string primaryRole = roleData.Roles.First();

                // Store ALL role properties in session (dynamic, from database)
                RoleHelper.SetSessionFromRoleData(HttpContext, roleData, primaryRole);

                // Redirect based on StartPage from role definition (dynamic)
                if (!string.IsNullOrEmpty(roleData.StartPage))
                {
                    var parts = roleData.StartPage.Split('/');
                    if (parts.Length == 2)
                    {
                        return RedirectToAction(parts[1], parts[0]);
                    }
                    if (roleData.StartPage.StartsWith("/"))
                        return Redirect(roleData.StartPage);
                }

                // Dynamic redirects based on role scope flags (from database)
                if (roleData.HierarchyLevel == 0)
                {
                    // Top-level admin → main dashboard
                    return RedirectToAction("Companies", "Companies");
                }
                else if (roleData.RequiresCompany && roleData.CompanyId.HasValue)
                {
                    // Company-scoped role → company details
                    var companyMapping = roleData.Mappings?.FirstOrDefault(m =>
                        m.CompanyId.HasValue && m.CompanyId.Value == roleData.CompanyId.Value);
                    string companyName = companyMapping?.ScopeName ?? $"Company {roleData.CompanyId.Value}";
                    HttpContext.Session.SetString("companyName", companyName);

                    return RedirectToAction("GroupsDetails", "Companies", new
                    {
                        id = roleData.CompanyId.Value,
                        companyName = companyName
                    });
                }
                else if (roleData.RequiresDevice)
                {
                    // Device-scoped role → device summary
                    var deviceMapping = roleData.Mappings?.FirstOrDefault(m => !string.IsNullOrEmpty(m.ScopeName));
                    var assignedDomain = deviceMapping?.ScopeName;

                    if (!string.IsNullOrEmpty(assignedDomain))
                    {
                        HttpContext.Session.SetString("assignedDomain", assignedDomain);
                        return RedirectToAction("Index", "ComputerSummary", new { domain = assignedDomain });
                    }
                }

                // Fallback: redirect to first authorized menu
                var dynamicMenus = await RoleHelper.GetDynamicMenusAsync(HttpContext);
                // Menus = page visibility only, no permission check needed
                var firstAuthorizedMenu = dynamicMenus
                    .OrderBy(m => m.SortOrder)
                    .FirstOrDefault();

                if (firstAuthorizedMenu != null && !string.IsNullOrEmpty(firstAuthorizedMenu.RouteController) && !string.IsNullOrEmpty(firstAuthorizedMenu.RouteAction))
                {
                    return RedirectToAction(firstAuthorizedMenu.RouteAction, firstAuthorizedMenu.RouteController);
                }

                return RedirectToAction("Companies", "Companies");
            }
            catch (Exception ex)
            {
                TempData["msg"] = $"Login Error: {ex.Message}";
                return View(model);
            }
        }
        [AllowAnonymous]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? requiredPermission = null)
        {
            ViewBag.RequiredPermission = requiredPermission;
            ViewBag.UserRole = HttpContext.Session.GetString("role");
            ViewBag.UserPermissions = HttpContext.Session.GetString("permissions");
            return View();
        }
        [HttpGet]
        [AuthFilter]
        [SuperAdminOnlyFilter]
        [DynamicPermission("Auth.ManageRoles", "Manage User Roles")]
        public async Task<IActionResult> ManageRoles()
        {
            if (!RoleHelper.IsTopLevelAdmin(HttpContext))
            {
                return RedirectToAction("AccessDenied");
            }
            var roles = await RoleHelper.GetAllRolesAsync();
            return View(roles);
        }
        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid request payload. Please check your inputs." });
                }

                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
            

                if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Role))
                {
                    return Json(new { success = false, message = "Username and Role are required" });
                }

                var success = await RoleHelper.AssignRoleAsync(model.Username!, model.Role!, model.CompanyId, model.DomainName, model.GroupId, model.LocationId);

                if (success)
                {
                    return Json(new { success = true, message = "Role assigned successfully" });
                }
                return Json(new { success = false, message = "Failed to assign role" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetAllSystemRoles()
        {
            try
            {
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new List<object>());
                }

                var roles = await RoleHelper.GetAllSystemRolesAsync();
                return Json(roles);
            }
            catch (Exception)
            {
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest model)
        {
            try
            {
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (string.IsNullOrWhiteSpace(model.RoleName))
                {
                    return Json(new { success = false, message = "Role name is required" });
                }

                var result = await RoleHelper.CreateRoleAsync(model.RoleName, model.Description,
                    model.RequiresCompany, model.RequiresDevice, model.RequiresLocation);

                if (result.Success)
                {
                    return Json(new { success = true, message = "Role created successfully" });
                }
                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeleteRole([FromBody] DeleteRoleRequest model)
        {
            try
            {
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Check if role is a system role dynamically (from API/DB)
                var systemRoles = await RoleHelper.GetAllSystemRolesAsync();
                var roleToDelete = systemRoles.FirstOrDefault(r => r.Name == model.RoleName);
                if (roleToDelete?.IsSystem == true)
                {
                    return Json(new { success = false, message = "Cannot delete system roles" });
                }

                var success = await RoleHelper.DeleteRoleAsync(model.RoleName);

                if (success)
                {
                    return Json(new { success = true, message = "Role deleted successfully" });
                }
                return Json(new { success = false, message = "Failed to delete role" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest model)
        {
            try
            {
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (string.IsNullOrEmpty(model.Username))
                {
                    return Json(new { success = false, message = "Username is required" });
                }

                var success = await RoleHelper.RemoveRoleAsync(model.Username);
                if (success)
                {
                    return Json(new { success = true, message = "Role removed successfully" });
                }
                return Json(new { success = false, message = "Failed to remove role" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetGroupedPermissions()
        {
            try
            {
                using var client = GetClient();

                var response = await client.GetAsync($"{_baseUrl}/api/Permission/Grouped");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                var errorMsg = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Backend Error: {errorMsg}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetMenusGroupedByModule()
        {
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Permission/MenusGroupedByModule");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                var errorMsg = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Backend Error: {errorMsg}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AuthFilter]
        [SuperAdminOnlyFilter]
        [DynamicPermission("Auth.ManagePermissions", "Manage System Permissions")]
        public IActionResult ManagePermissions()
        {
            if (!RoleHelper.IsTopLevelAdmin(HttpContext)) return RedirectToAction("Login");
            return View();
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetStats() {
            try {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/Permission/Stats");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetMenus() {
            try {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/Permission/Menus");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return StatusCode(500, "Failed to fetch menus: " + ex.Message); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SaveMenu() {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/Permission/Menus", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                try {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    if (result?.success != null) return Content(responseContent, "application/json");
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Menu saved successfully", data = result });
                    return Json(new { success = false, message = (string)(result?.message ?? result?.Message ?? $"API Error: {response.StatusCode}") });
                } catch {
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Menu saved successfully" });
                    return Json(new { success = false, message = $"API Error {(int)response.StatusCode}: {responseContent}" });
                }
            } catch (TaskCanceledException) { return Json(new { success = false, message = "API Server not responding. Is ManageEngineSoftware running?" }); }
            catch (HttpRequestException) { return Json(new { success = false, message = "Cannot connect to API Server at " + _baseUrl }); }
            catch (Exception ex) { return Json(new { success = false, message = "WebApp error: " + ex.Message }); }
        }

        [HttpDelete]
        [AuthFilter]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            try {
                var response = await GetClient().DeleteAsync($"{_baseUrl}/api/Permission/Menus/{id}");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "Menu deleted successfully" });

                try {
                    var err = JsonConvert.DeserializeObject<dynamic>(content);
                    return Json(new { success = false, message = (string)err.message ?? "API failure" });
                } catch {
                    return Json(new { success = false, message = "Failed to delete menu in API" });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetModules() {
            try {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/Permission/Modules");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (TaskCanceledException) {
                return StatusCode(500, "API Server not responding. Please ensure ManageEngineSoftware is running on " + _baseUrl);
            } catch (HttpRequestException) {
                return StatusCode(500, "Cannot connect to API Server at " + _baseUrl + ". Please start the backend API.");
            } catch (Exception ex) {
                return StatusCode(500, "Failed to fetch modules: " + ex.Message);
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SaveModule() {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/Permission/Modules", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                try {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    if (result?.success != null) return Content(responseContent, "application/json");
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Module saved successfully", data = result });
                    return Json(new { success = false, message = (string)(result?.message ?? result?.Message ?? $"API Error: {response.StatusCode}") });
                } catch {
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Module saved successfully" });
                    return Json(new { success = false, message = $"API Error {(int)response.StatusCode}: {responseContent}" });
                }
            } catch (TaskCanceledException) { return Json(new { success = false, message = "API Server not responding. Is ManageEngineSoftware running?" }); }
            catch (HttpRequestException) { return Json(new { success = false, message = "Cannot connect to API Server at " + _baseUrl }); }
            catch (Exception ex) { return Json(new { success = false, message = "WebApp error: " + ex.Message }); }
        }

        [HttpDelete]
        [AuthFilter]
        public async Task<IActionResult> DeleteModule(int id) {
            try {
                var response = await GetClient().DeleteAsync($"{_baseUrl}/api/Permission/Modules/{id}");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "Module deleted successfully" });

                try {
                    var err = JsonConvert.DeserializeObject<dynamic>(content);
                    return Json(new { success = false, message = (string)err.message ?? "API failure" });
                } catch {
                    return Json(new { success = false, message = "Failed to delete module in API" });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetPermissions() {
            try {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/Permission/List");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return StatusCode(500, "Failed to fetch permissions: " + ex.Message); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SavePermission() {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/Permission/SavePermission", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                try {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    if (result?.success != null) return Content(responseContent, "application/json");
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Permission saved successfully", data = result });
                    return Json(new { success = false, message = (string)(result?.message ?? result?.Message ?? $"API Error: {response.StatusCode}") });
                } catch {
                    if (response.IsSuccessStatusCode)
                        return Json(new { success = true, message = "Permission saved successfully" });
                    return Json(new { success = false, message = $"API Error {(int)response.StatusCode}: {responseContent}" });
                }
            } catch (TaskCanceledException) { return Json(new { success = false, message = "API Server not responding. Is ManageEngineSoftware running?" }); }
            catch (HttpRequestException) { return Json(new { success = false, message = "Cannot connect to API Server at " + _baseUrl }); }
            catch (Exception ex) { return Json(new { success = false, message = "WebApp error: " + ex.Message }); }
        }

        [HttpDelete]
        [AuthFilter]
        public async Task<IActionResult> DeletePermission(int id) {
            try {
                var response = await GetClient().DeleteAsync($"{_baseUrl}/api/Permission/{id}");
                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = "Permission deleted successfully" });

                try {
                    var err = JsonConvert.DeserializeObject<dynamic>(content);
                    return Json(new { success = false, message = (string)err.message ?? "API failure" });
                } catch {
                    return Json(new { success = false, message = "Failed to delete permission in API" });
                }
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }



        [HttpPost]
        [AuthFilter]
        [DynamicPermission("Auth.SeedPermissions", "Seed/Reset Permissions")]
        public async Task<IActionResult> SeedPermissions()
        {
            try
            {
                var discoveredPermissions = _permissionDiscovery.DiscoverPermissions();
                int permCount = 0;
                int moduleCount = 0;
                var errors = new List<string>();

                // 0. Ensure SuperAdmin role definition exists with HierarchyLevel 0
                await RoleHelper.CreateRoleAsync("SuperAdmin", "Top-level System Administrator with full access", false, false, false);

                // 1. Seed Modules First
                var uniqueModules = discoveredPermissions.Select(p => p.Module).Distinct().Where(m => !string.IsNullOrEmpty(m));
                foreach (var modName in uniqueModules)
                {
                    try {
                        var modContent = new StringContent(JsonConvert.SerializeObject(new {
                            ModuleName = modName,
                            DisplayName = modName,
                            Description = $"Module for {modName}",
                            IconClass = "fas fa-cube",
                            IsActive = true
                        }), Encoding.UTF8, "application/json");

                        var res = await GetClient().PostAsync($"{_baseUrl}/api/Permission/Modules", modContent);
                        if (res.IsSuccessStatusCode) moduleCount++;
                        else errors.Add($"Module {modName}: {await res.Content.ReadAsStringAsync()}");
                    } catch (Exception ex) { errors.Add($"Module {modName}: {ex.Message}"); }
                }

                // 2. Seed Permissions
                foreach (var p in discoveredPermissions)
                {
                    try {
                        var content = new StringContent(JsonConvert.SerializeObject(new {
                            Module = p.Module,
                            PermissionCode = p.PermissionCode,
                            PermissionName = p.PermissionName,
                            ActionType = p.ActionType,
                            Description = p.Description,
                            Category = p.Module,
                            ResourceType = "Action",
                            SortOrder = p.SortOrder,
                            RouteController = p.Controller,
                            RouteAction = p.Action,
                            IsActive = true
                        }), Encoding.UTF8, "application/json");

                        var res = await GetClient().PostAsync($"{_baseUrl}/api/Permission/SavePermission", content);
                        if (res.IsSuccessStatusCode) permCount++;
                        // Don't log "Exists" errors as failures, but track others
                    } catch (Exception ex) { errors.Add($"Perm {p.PermissionCode}: {ex.Message}"); }
                }

                // 3. Assign All Permissions to SuperAdmin
                var allCodes = discoveredPermissions.Select(p => p.PermissionCode).ToList();
                var assignRequest = new { roleName = "SuperAdmin", permissionCodes = allCodes, assignedBy = "System" };
                var assignContent = new StringContent(JsonConvert.SerializeObject(assignRequest), Encoding.UTF8, "application/json");
                await GetClient().PostAsync($"{_baseUrl}/api/Permission/AssignToRole", assignContent);

                // 4. Assign All Menus to SuperAdmin
                string menuMsg = "";
                var menusRes = await GetClient().GetAsync($"{_baseUrl}/api/Permission/Menus");
                if (menusRes.IsSuccessStatusCode)
                {
                    var menusJson = await menusRes.Content.ReadAsStringAsync();
                    var menus = JsonConvert.DeserializeObject<List<MenuDefinitionDto>>(menusJson);
                    if (menus != null)
                    {
                        var menuIds = menus.Select(m => m.Id).ToList();
                        var menuPayload = new { roleName = "SuperAdmin", menuIds = menuIds, assignedBy = "System" };
                        var menuContent = new StringContent(JsonConvert.SerializeObject(menuPayload), Encoding.UTF8, "application/json");
                        var menuRes = await GetClient().PostAsync($"{_baseUrl}/api/Permission/AssignMenusToRole", menuContent);
                        if (menuRes.IsSuccessStatusCode) menuMsg = " and all menus";
                    }
                }

                // 5. Refresh current user's session
                await RoleHelper.RefreshSessionPermissionsAsync(HttpContext);

                return Json(new {
                    success = true,
                    message = $"Seeded {moduleCount} modules and {permCount} permissions. SuperAdmin now has full access to all permissions{menuMsg}.",
                    details = errors
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Seeding failed: " + ex.Message });
            }
        }

        [HttpGet]
        [AuthFilter]
        public IActionResult GetDiscoveredRoutes()
        {
            try
            {
                var discovered = _permissionDiscovery.DiscoverPermissions();
                var routes = discovered.Select(d => new {
                    controller = d.Controller,
                    action = d.Action,
                    module = d.Module,
                    permission = d.PermissionCode
                }).Distinct().OrderBy(r => r.controller).ThenBy(r => r.action).ToList();
                return Json(routes);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> CreateRoleWithPermissions([FromBody] CreateRoleWithPermissionsRequest model)
        {
            try
            {
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (string.IsNullOrWhiteSpace(model.RoleName))
                {
                    return Json(new { success = false, message = "Role name is required" });
                }

                if (model.Permissions == null || !model.Permissions.Any())
                {
                    return Json(new { success = false, message = "At least one permission is required" });
                }

                var roleResult = await RoleHelper.CreateRoleAsync(
                    model.RoleName,
                    model.Description,
                    model.RequiresCompany,
                    model.RequiresDevice,
                    model.RequiresLocation,
                    model.RequiresGroup);

                if (!roleResult.Success)
                {
                    return Json(new { success = false, message = roleResult.Message });
                }

                // 2. Assign Permissions to the new role
                using var client = GetClient();

                var permPayload = new
                {
                    roleName = model.RoleName,
                    permissionCodes = model.Permissions,
                    assignedBy = HttpContext.Session.GetString("username") ?? "System"
                };
                var content = new StringContent(
                    Newtonsoft.Json.JsonConvert.SerializeObject(permPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var permResponse = await client.PostAsync($"{_baseUrl}/api/Permission/AssignToRole", content);
                if (!permResponse.IsSuccessStatusCode)
                {
                    var permError = await permResponse.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Role created but permission assignment failed: {permError}" });
                }

                // 3. Assign Menus
                if (model.MenuIds != null && model.MenuIds.Any())
                {
                    var menuPayload = new
                    {
                        roleName = model.RoleName,
                        menuIds = model.MenuIds
                    };
                    var menuContent = new StringContent(
                        Newtonsoft.Json.JsonConvert.SerializeObject(menuPayload),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var menuResponse = await client.PostAsync($"{_baseUrl}/api/Permission/AssignMenusToRole", menuContent);
                    if (!menuResponse.IsSuccessStatusCode)
                    {
                        var menuError = await menuResponse.Content.ReadAsStringAsync();
                        return Json(new { success = true, message = $"Role created with permissions, but menu assignment failed: {menuError}" });
                    }
                }

                return Json(new { success = true, message = $"Role '{model.RoleName}' created with {model.Permissions.Count} permissions and {model.MenuIds?.Count ?? 0} menus." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetRoleDetails(string roleName)
        {
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Permission/RoleDetails/{roleName}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
                return NotFound("Role details not found in backend");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdateRoleWithPermissions([FromBody] CreateRoleWithPermissionsRequest model)
        {
            try
            {
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (string.IsNullOrWhiteSpace(model.RoleName))
                {
                    return Json(new { success = false, message = "Role name is required" });
                }

                // 1. Update Role Definition in backend
                using var client = GetClient();
                var roleUpdatePayload = new {
                    RoleName = model.RoleName,
                    Description = model.Description,
                    RequiresCompany = model.RequiresCompany,
                    RequiresGroup = model.RequiresGroup,
                    RequiresDevice = model.RequiresDevice,
                    RequiresLocation = model.RequiresLocation,
                    HierarchyLevel = (model.RoleName == "SuperAdmin") ? 0 : 10
                };
                var roleContent = new StringContent(JsonConvert.SerializeObject(roleUpdatePayload), Encoding.UTF8, "application/json");
                var roleResponse = await client.PostAsync($"{_baseUrl}/api/Auth/role/create", roleContent);
                if (!roleResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Failed to update role definition: " + await roleResponse.Content.ReadAsStringAsync() });
                }

                // 2. Assign Permissions
                if (model.Permissions != null)
                {
                    var permPayload = new {
                        roleName = model.RoleName,
                        permissionCodes = model.Permissions,
                        assignedBy = HttpContext.Session.GetString("username") ?? "System"
                    };
                    var permContent = new StringContent(JsonConvert.SerializeObject(permPayload), Encoding.UTF8, "application/json");
                    await client.PostAsync($"{_baseUrl}/api/Permission/AssignToRole", permContent);
                }

                // 3. Assign Menus
                if (model.MenuIds != null)
                {
                    var menuPayload = new {
                        roleName = model.RoleName,
                        menuIds = model.MenuIds
                    };
                    var menuContent = new StringContent(JsonConvert.SerializeObject(menuPayload), Encoding.UTF8, "application/json");
                    await client.PostAsync($"{_baseUrl}/api/Permission/AssignMenusToRole", menuContent);
                }

                return Json(new { success = true, message = "Role updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class AssignRoleRequest
    {
        public string? Username { get; set; }
        public string? Role { get; set; }
        public int? CompanyId { get; set; }
        public int? GroupId { get; set; }
        public string? DomainName { get; set; }
        public int? LocationId { get; set; }
    }

    public class RemoveRoleRequest
    {
        public string? Username { get; set; }
    }

    public class CreateRoleRequest
    {
        public string? RoleName { get; set; }
        public string? Description { get; set; }
        public bool RequiresCompany { get; set; }
        public bool RequiresDevice { get; set; }
        public bool RequiresLocation { get; set; }
    }

    public class CreateRoleWithPermissionsRequest
    {
        public string? RoleName { get; set; }
        public string? Description { get; set; }
        public bool RequiresCompany { get; set; }
        public bool RequiresGroup { get; set; }
        public bool RequiresDevice { get; set; }
        public bool RequiresLocation { get; set; }
        public List<string>? Permissions { get; set; }
        public List<int>? MenuIds { get; set; }
    }

    public class DeleteRoleRequest
    {
        public string? RoleName { get; set; }
    }
}
