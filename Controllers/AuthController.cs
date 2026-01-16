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
        private readonly string apiBaseUrl = "https://172.16.15.15:4431/api/auth";

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
                TempData["msg"] = "Invalid username or password.";
                return View(model);
            }
            try
            {
         
                var roleData = await RoleHelper.GetUserRoleFromApiAsync(model.Username);
                if (roleData == null || string.IsNullOrEmpty(roleData.Role))
                {
                    TempData["msg"] = "Your account has no role assigned. Contact your administrator.";
                    return View(model);
                }
  
                HttpContext.Session.SetString("username", model.Username);
                HttpContext.Session.SetString("role", roleData.Role);
                if (roleData.CompanyId.HasValue)
                {
                    HttpContext.Session.SetString("companyId", roleData.CompanyId.Value.ToString());
                }
                if (!string.IsNullOrEmpty(roleData.CompanyName))
                {
                    HttpContext.Session.SetString("companyName", roleData.CompanyName);
                }
            
                if (roleData.Role == "SuperAdmin")
                {
                    return RedirectToAction("Companies", "Companies");
                }
                else if (roleData.Role == "CompanyAdmin" && roleData.CompanyId.HasValue)
                {
                    return RedirectToAction("GroupsDetails", "Companies", new
                    {
                        id = roleData.CompanyId.Value,
                        companyName = roleData.CompanyName ?? $"Company {roleData.CompanyId.Value}"
                    });
                }
                else if (roleData.Role == "CompanyUser")
                {
                    HttpContext.Session.SetString("role", "CompanyUser");

                    if (roleData.CompanyId.HasValue)
                    {
                        HttpContext.Session.SetInt32("companyId", roleData.CompanyId.Value);
                    }

                    var assignedDomain = roleData.DomainName;

                    if (!string.IsNullOrEmpty(assignedDomain))
                    {
                        HttpContext.Session.SetString("assignedDomain", assignedDomain);
                        return RedirectToAction("Index", "ComputerSummary", new { domain = assignedDomain });
                    }
                    else
                    {
                        ViewBag.Error = "No device assigned to your account. Contact administrator.";
                        return View();
                    }
                }
                TempData["msg"] = "Your account has an unrecognized role.";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["msg"] = $"Error: {ex.Message}";
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

                var success = await RoleHelper.AssignRoleAsync(model.Username, model.Role, model.CompanyId, model.DomainName);

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

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest model)
        {
            if (!RoleHelper.IsSuperAdmin(HttpContext))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            var success = await RoleHelper.RemoveRoleAsync(model.Username);
            if (success)
            {
                return Json(new { success = true, message = "Role removed successfully" });
            }
            return Json(new { success = false, message = "Failed to remove role" });
        }
    }
    public class AssignRoleRequest
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public int? CompanyId { get; set; }
        public string DomainName { get; set;  }
    }
    public class RemoveRoleRequest
    {
        public string Username { get; set; }
    }
}