using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using static ManageEngineWebApp.Datacontext.RoleHelper;

namespace ManageEngineWebApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly string apiBaseUrl;
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
            apiBaseUrl = $"{baseUrl}/api/auth";
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using var client = new HttpClient(handler);
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

            var currentUserRole = HttpContext.Session.GetString("role");
            if (currentUserRole == "SuperAdmin" && !string.IsNullOrEmpty(model.Role))
            {
                if (model.Role == "CompanyAdmin" && model.CompanyId.HasValue)
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
            }
            else
            {
                TempData["msg"] = "Registration successful. Please wait for role assignment.";
            }

            if (currentUserRole == "SuperAdmin")
            {
                return RedirectToAction("ManageRoles");
            }
            return RedirectToAction("Login");
        }
        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client, NoStore = false)]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using var client = new HttpClient(handler);
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
                var roleData = await RoleHelper.GetUserRoleFromApiAsync(model.Username);
                
                if (roleData == null)
                {
                     TempData["msg"] = "Failed to retrieve user roles. API may be offline or returning errors.";
                     return View(model);
                }

                if (roleData.Roles == null || !roleData.Roles.Any())
                {
                    TempData["msg"] = "Your account has no role assigned. Contact your administrator.";
                    return View(model);
                }

                string primaryRole = roleData.Roles.Contains("SuperAdmin") ? "SuperAdmin" :
                                    (roleData.Roles.Contains("CompanyAdmin") ? "CompanyAdmin" : roleData.Roles.First());

                HttpContext.Session.SetString("username", model.Username);
                HttpContext.Session.SetString("role", primaryRole);

                if (roleData.CompanyId.HasValue)
                {
                    HttpContext.Session.SetString("companyId", roleData.CompanyId.Value.ToString());
                }

                if (roleData.Permissions != null && roleData.Permissions.Any())
                {
                    HttpContext.Session.SetString("permissions", string.Join(",", roleData.Permissions));
                }

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

                if (primaryRole == "SuperAdmin")
                {
                    return RedirectToAction("Companies", "Companies");
                }
                else if (primaryRole == "CompanyAdmin" && roleData.CompanyId.HasValue)
                {
                    var companyMapping = roleData.Mappings?.FirstOrDefault(m => m.RoleName == "CompanyAdmin");
                    string companyName = companyMapping?.ScopeName ?? $"Company {roleData.CompanyId.Value}";
                    HttpContext.Session.SetString("companyName", companyName);

                    return RedirectToAction("GroupsDetails", "Companies", new
                    {
                        id = roleData.CompanyId.Value,
                        companyName = companyName
                    });
                }
                else if (primaryRole == "CompanyUser") 
                {
                    HttpContext.Session.SetString("role", "CompanyUser");

                    if (roleData.CompanyId.HasValue)
                    {
                        HttpContext.Session.SetInt32("companyId", roleData.CompanyId.Value);
                    }

                    var deviceMapping = roleData.Mappings?.FirstOrDefault(m => m.RoleName == "CompanyUser" && !string.IsNullOrEmpty(m.ScopeName));
                    var assignedDomain = deviceMapping?.ScopeName;

                    if (!string.IsNullOrEmpty(assignedDomain))
                    {
                        HttpContext.Session.SetString("assignedDomain", assignedDomain);
                        return RedirectToAction("Index", "ComputerSummary", new { domain = assignedDomain });
                    }
                    else
                    {
                      
                    }
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["msg"] = $"Login Error: {ex.Message}";
                return View(model);
            }
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> ManageRoles()
        {
            if (!RoleHelper.IsSuperAdmin(HttpContext))
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
                if (!RoleHelper.IsSuperAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }
                if (model.Role == "SuperAdmin")
                {
                    return Json(new { success = false, message = "Cannot assign SuperAdmin role" });
                }
                if (model.Role == "CompanyAdmin" && !model.CompanyId.HasValue)
                {
                    return Json(new { success = false, message = "Company Admin must be assigned to a company" });
                }

                if (model.Role == "CompanyUser" && string.IsNullOrEmpty(model.DomainName))
                {
                    return Json(new { success = false, message = "Company User must be assigned to a device" });
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
                if (!RoleHelper.IsSuperAdmin(HttpContext))
                {
                    return Json(new List<object>());
                }

                var roles = await RoleHelper.GetAllSystemRolesAsync();
                return Json(roles);
            }
            catch (Exception ex)
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
                if (!RoleHelper.IsSuperAdmin(HttpContext))
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
                if (!RoleHelper.IsSuperAdmin(HttpContext))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (model.RoleName == "SuperAdmin" || model.RoleName == "CompanyAdmin" || model.RoleName == "CompanyUser")
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
                if (!RoleHelper.IsSuperAdmin(HttpContext))
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

    public class DeleteRoleRequest
    {
        public string? RoleName { get; set; }
    }
}
