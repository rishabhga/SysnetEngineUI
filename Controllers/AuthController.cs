using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Services;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using static ManageEngineWebApp.Datacontext.RoleHelper;
using ManageEngineWebApp.Requests;
using ManageEngineWebApp.Filters;
using ManageEngineWebApp.Models;

namespace ManageEngineWebApp.Controllers
{
    public class AuthController : BaseController
    {
        private bool HasPerm(string code) => RoleHelper.HasPermission(HttpContext, code);
        private readonly string apiBaseUrl;
        private readonly PermissionDiscoveryService _permissionDiscovery;
        private readonly IEmailService _emailService;

        public AuthController(IConfiguration configuration, PermissionDiscoveryService permissionDiscovery, IHttpClientFactory httpClientFactory, IEmailService emailService) : base(httpClientFactory, configuration)
        {
            _permissionDiscovery = permissionDiscovery;
            _emailService = emailService;
            apiBaseUrl = $"{_baseUrl}/api/auth";
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCompaniesForRegister()
        {
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Companiesdata");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<Companies>>(content) ?? new List<Companies>();
                    return Json(data);
                }
                return Json(new { error = true, message = $"API Error: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = $"WebApp Error: {ex.Message}" });
            }
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.CompanyId <= 0)
            {
                ModelState.AddModelError("CompanyId", "Please select a valid company.");
                return View(model);
            }

            using var client = GetClient();
            var registerPayload = new
            {
                model.Username,
                model.Email,
                model.Password,
                model.ConfirmPassword,
                model.CompanyId
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

            TempData["msg"] = "Registration successful. Please wait for admin role assignment.";
            return RedirectToAction("Login");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            HttpContext.Session.Clear();

            foreach (var cookieName in HttpContext.Request.Cookies.Keys)
            {
                if (!cookieName.StartsWith("__RequestVerification") && cookieName != "RememberMe_User")
                {
                    Response.Cookies.Delete(cookieName);
                }
            }

            var rememberedUser = Request.Cookies["RememberMe_User"];
            var model = new LoginDto 
            { 
                Username = rememberedUser, 
                RememberMe = !string.IsNullOrEmpty(rememberedUser) 
            };
            return View(model);
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

            var apiResultStr = await response.Content.ReadAsStringAsync();
            dynamic apiResult;
            try
            {
                apiResult = JsonConvert.DeserializeObject<dynamic>(apiResultStr);
                string token = apiResult?.token;
                if (!string.IsNullOrEmpty(token))
                {
                    HttpContext.Session.SetString("JwtToken", token);
                }
            }
            catch (JsonReaderException)
            {
                var preview = apiResultStr.Length > 100 ? apiResultStr.Substring(0, 100) + "..." : apiResultStr;
                TempData["msg"] = $"API error: Unexpected response format. Content: {preview}";
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

                string primaryRole = roleData.Roles.First();
                RoleHelper.SetSessionFromRoleData(HttpContext, roleData, primaryRole);

                if (model.RememberMe)
                {
                    Response.Cookies.Append("RememberMe_User", model.Username ?? "", new CookieOptions
                    {
                        Expires = DateTimeOffset.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        IsEssential = true
                    });
                }
                else
                {
                    Response.Cookies.Delete("RememberMe_User");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["msg"] = "An error occurred during login. Please try again.";
                return View(model);
            }
        }
        [AllowAnonymous]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            foreach (var cookieName in HttpContext.Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookieName);
            }
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                using var client = GetClient();
                var response = await client.PostAsJsonAsync($"{apiBaseUrl}/forgot-password", new { model.Email });

                if (response.IsSuccessStatusCode)
                {
                    var contentStr = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(contentStr);
                    string token = result.token;
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        var resetLink = Url.Action("ResetPassword", "Auth", new { token, email = model.Email }, Request.Scheme);
                        
                        await _emailService.SendEmailAsync(model.Email, "Reset Password - SYSNET", 
                            $"Please reset your password by clicking here: <a href='{resetLink}'>Reset Password</a>");
                    }

                    ViewBag.Message = "If an account with that email exists, we have sent a password reset link.";
                    return View();
                }
                
                ViewBag.Message = "If an account with that email exists, we have sent a password reset link.";
                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error sending reset email: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                using var client = GetClient();
                var response = await client.PostAsJsonAsync($"{apiBaseUrl}/reset-password", new 
                { 
                    model.Email, 
                    model.Token, 
                    model.Password 
                });

                if (response.IsSuccessStatusCode)
                {
                    TempData["msg"] = "Password has been reset successfully. You can now login.";
                    return RedirectToAction("Login");
                }
                
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", "Error resetting password: " + error);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return View(model);
            }
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
        [DynamicPermission("Auth.ManageRoles", "Manage User Roles")]
        public async Task<IActionResult> ManageRoles()
        {
            var query = BuildScopedQuery();
            var roles = await RoleHelper.GetAllRolesAsync(query);
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

                if (!HasPerm("Auth.ManageRoles"))
                {
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });
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
                if (!HasPerm("Auth.ManageRoles"))
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
                if (!HasPerm("Auth.ManageRoles"))
                {
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });
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
                if (!HasPerm("Auth.ManageRoles"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });

                var systemRoles = await RoleHelper.GetAllSystemRolesAsync();
                var roleToDelete = systemRoles.FirstOrDefault(r => r.Name == model.RoleName);
                if (roleToDelete?.IsSystem == true)
                    return Json(new { success = false, message = "Cannot delete system roles" });

                var success = await RoleHelper.DeleteRoleAsync(model.RoleName);
                return Json(success
                    ? new { success = true, message = "Role deleted successfully" }
                    : new { success = false, message = "Failed to delete role" });
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
                if (!HasPerm("Auth.ManageRoles"))
                {
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });
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

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> RemoveMapping(int id)
        {
            try
            {
                if (!HasPerm("Auth.ManageRoles"))
                {
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });
                }

                using var client = GetClient();
                var response = await client.PostAsync($"{apiBaseUrl}/role/remove-mapping/{id}", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Content(result, "application/json");
                }

                return Json(new { success = false, message = $"Backend Error: {response.StatusCode}" });
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
        [DynamicPermission("Auth.ManagePermissions", "Manage System Permissions")]
        public IActionResult ManagePermissions()
        {
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

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                using var client = GetClient();
                var response = await client.PostAsync($"{_baseUrl}/api/Permission/DeleteMenu/{id}", null);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
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

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                using var client = GetClient();
                var response = await client.PostAsync($"{_baseUrl}/api/Permission/DeleteModule/{id}", null);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetPermissions() {
            try {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/Permission/List");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return StatusCode(500, "Failed to fetch permissions: " + ex.Message); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetPolicyTemplates()
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new List<object>());

                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Policy/Templates");
                var payload = await response.Content.ReadAsStringAsync();
                return Content(payload, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to fetch policy templates: {ex.Message}" });
            }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetPolicyTemplateDetails(int id)
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Policy/Templates/{id}");
                var payload = await response.Content.ReadAsStringAsync();
                return Content(payload, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to fetch policy template details: {ex.Message}" });
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SavePolicyTemplate()
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(body))
                    return Json(new { success = false, message = "Request body is empty." });

                dynamic parsed = JsonConvert.DeserializeObject<dynamic>(body)!;
                int id = parsed?.Id != null ? (int)parsed.Id : 0;

                using var client = GetClient();
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage response = id > 0
                    ? await client.PutAsync($"{_baseUrl}/api/Policy/Templates/{id}", content)
                    : await client.PostAsync($"{_baseUrl}/api/Policy/Templates", content);

                var payload = await response.Content.ReadAsStringAsync();
                return Content(payload, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to save policy template: {ex.Message}" });
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeletePolicyTemplate(int id)
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                using var client = GetClient();
                var response = await client.DeleteAsync($"{_baseUrl}/api/Policy/Templates/{id}");
                var payload = await response.Content.ReadAsStringAsync();
                return Content(payload, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to delete policy template: {ex.Message}" });
            }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SetPolicyTemplatePermissions(int id, [FromBody] PolicyPermissionUpdateRequest request)
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                var payloadObj = new
                {
                    PermissionIds = request.PermissionIds ?? new List<int>(),
                    AssignedBy = HttpContext.Session.GetString("username") ?? "System"
                };

                using var client = GetClient();
                using var content = new StringContent(JsonConvert.SerializeObject(payloadObj), Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{_baseUrl}/api/Policy/Templates/{id}/Permissions", content);
                var payload = await response.Content.ReadAsStringAsync();
                return Content(payload, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to assign template permissions: {ex.Message}" });
            }
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

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeletePermission(int id)
        {
            try
            {
                if (!HasPerm("Auth.ManagePermissions"))
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManagePermissions permission" });

                using var client = GetClient();
                var response = await client.PostAsync($"{_baseUrl}/api/Permission/DeletePermission/{id}", null);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
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
                await RoleHelper.CreateRoleAsync("SuperAdmin", "Top-level System Administrator with full access", false, false, false);

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
                    } catch (Exception ex) { errors.Add($"Perm {p.PermissionCode}: {ex.Message}"); }
                }

                var allCodes = discoveredPermissions.Select(p => p.PermissionCode).ToList();
                var assignRequest = new { roleName = "SuperAdmin", permissionCodes = allCodes, assignedBy = "System" };
                var assignContent = new StringContent(JsonConvert.SerializeObject(assignRequest), Encoding.UTF8, "application/json");
                await GetClient().PostAsync($"{_baseUrl}/api/Permission/AssignToRole", assignContent);

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
                if (!HasPerm("Auth.ManageRoles"))
                {
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });
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

                if (model.MenuIds != null && model.MenuIds.Any())
                {
                    var menuPayload = new
                    {
                        roleName = model.RoleName,
                        menuIds = model.MenuIds,
                        assignedBy = HttpContext.Session.GetString("username") ?? "System"
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

                if (model.PolicyIds != null && model.PolicyIds.Any())
                {
                    var policyPayload = new
                    {
                        roleName = model.RoleName,
                        policyIds = model.PolicyIds,
                        assignedBy = HttpContext.Session.GetString("username") ?? "System"
                    };
                    var policyContent = new StringContent(
                        Newtonsoft.Json.JsonConvert.SerializeObject(policyPayload),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var policyResponse = await client.PostAsync($"{_baseUrl}/api/Permission/AssignPoliciesToRole", policyContent);
                    if (!policyResponse.IsSuccessStatusCode)
                    {
                        var policyError = await policyResponse.Content.ReadAsStringAsync();
                        return Json(new { success = true, message = $"Role created, but policy assignment failed: {policyError}" });
                    }
                }

                return Json(new { success = true, message = $"Role '{model.RoleName}' created with {model.Permissions.Count} permissions, {model.MenuIds?.Count ?? 0} menus, and {model.PolicyIds?.Count ?? 0} policies." });
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
                if (!HasPerm("Auth.ManageRoles"))
                {
                    return Json(new { success = false, message = "Unauthorized: Missing Auth.ManageRoles permission" });
                }

                if (string.IsNullOrWhiteSpace(model.RoleName))
                {
                    return Json(new { success = false, message = "Role name is required" });
                }

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

                if (model.MenuIds != null)
                {
                    var menuPayload = new {
                        roleName = model.RoleName,
                        menuIds = model.MenuIds,
                        assignedBy = HttpContext.Session.GetString("username") ?? "System"
                    };
                    var menuContent = new StringContent(JsonConvert.SerializeObject(menuPayload), Encoding.UTF8, "application/json");
                    await client.PostAsync($"{_baseUrl}/api/Permission/AssignMenusToRole", menuContent);
                }

                if (model.PolicyIds != null)
                {
                    var policyPayload = new {
                        roleName = model.RoleName,
                        policyIds = model.PolicyIds,
                        assignedBy = HttpContext.Session.GetString("username") ?? "System"
                    };
                    var policyContent = new StringContent(JsonConvert.SerializeObject(policyPayload), Encoding.UTF8, "application/json");
                    await client.PostAsync($"{_baseUrl}/api/Permission/AssignPoliciesToRole", policyContent);
                }

                return Json(new { success = true, message = "Role updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AuthFilter]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var username = HttpContext.Session.GetString("username");
                if (string.IsNullOrEmpty(username)) return RedirectToAction("Login");

                using var client = GetClient();
                var payload = new 
                {
                    Username = username,
                    OldPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/Auth/UpdatePassword", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMsg"] = "Password updated successfully!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Failed to update password: " + error);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return View(model);
            }
        }
    }
}
