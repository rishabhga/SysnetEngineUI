using ManageEngineWebApp.Attributes;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace ManageEngineWebApp.Controllers
{

    [AuthFilter]
    public class CompaniesController : BaseController
    {
        private readonly IWebHostEnvironment _env;

        public CompaniesController(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, IConfiguration configuration)
            : base(httpClientFactory, configuration)
        {
            _env = env;
        }

        public IActionResult Index() => RedirectToAction("Companies");

        public async Task<IActionResult> Companies()
        {
            var username = HttpContext.Session.GetString("username");
            if (RoleHelper.IsCompanyScopedRole(HttpContext))
            {
                var companyIds = RoleHelper.GetCompanyIds(HttpContext);
                if (companyIds.Count == 1)
                {
                    return RedirectToAction("GroupsDetails", new { id = companyIds.First() });
                }
            }

            var activeComputers = new List<string>();
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Command/GetConnectedDevices");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var clients = JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content);
                    if (clients != null)
                    {
                        activeComputers = clients
                            .Select(c => c.UserName?.Trim().ToUpper() ?? "")
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            ViewBag.ActiveComputers = activeComputers;
            ViewBag.ApiBaseUrl = _baseUrl;

            var hierarchyData = await LoadHierarchyAsync();
            return View(hierarchyData ?? new List<CompanyHierarchyDto>());
        }


        [HttpGet]
        public async Task<IActionResult> GetCompaniesForDropdown()
        {
            var data = new List<Companies>();
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Companiesdata");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content)
                        ? JsonConvert.DeserializeObject<List<Companies>>(content) ?? new List<Companies>()
                        : new List<Companies>();
                }

                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    var userCompanyIds = RoleHelper.GetCompanyIds(HttpContext);
                    data = data.Where(c => userCompanyIds.Contains(c.Id)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetCompaniesForDropdown Error: {ex.Message}");
            }
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupsByCompany(int companyId)
        {
            if (!RoleHelper.ValidateScope(HttpContext, companyId))
                return Json(new { success = false, message = "Access Denied: You don't have access to this company." });

            using var client = GetClient();
            var query = BuildScopedQuery(companyId);
            var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata{query}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var groups = Newtonsoft.Json.Linq.JArray.Parse(json);
                if (companyId > 0)
                {
                    groups = new Newtonsoft.Json.Linq.JArray(groups.Where(g =>
                    {
                        var gCompId = g["companyID"]?.Value<int?>()
                                   ?? g["CompanyID"]?.Value<int?>()
                                   ?? g["companyId"]?.Value<int?>()
                                   ?? g["CompanyId"]?.Value<int?>()
                                   ?? 0;
                        return gCompId == companyId;
                    }));
                }
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    var userGroupIds = RoleHelper.GetGroupIds(HttpContext);
                    if (userGroupIds.Any())
                    {
                        groups = new Newtonsoft.Json.Linq.JArray(groups.Where(g =>
                        {
                            var id = g["id"]?.Value<int>() ?? g["Id"]?.Value<int>() ?? 0;
                            return userGroupIds.Contains(id);
                        }));
                    }
                }

                return Content(groups.ToString(Newtonsoft.Json.Formatting.None), "application/json");
            }
            return Json(new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationsByGroup(int companyId, int groupId)
        {
            if (!RoleHelper.ValidateScope(HttpContext, companyId, groupId))
                return Json(new { success = false, message = "Access Denied" });

            using var client = GetClient();
            var query = BuildScopedQuery(companyId, null, groupId);
            var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata{query}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var locations = Newtonsoft.Json.Linq.JArray.Parse(json);
                if (groupId > 0)
                {
                    locations = new Newtonsoft.Json.Linq.JArray(locations.Where(l =>
                    {
                        var lGroupId = l["groupsID"]?.Value<int?>()
                                    ?? l["GroupsID"]?.Value<int?>()
                                    ?? l["groupId"]?.Value<int?>()
                                    ?? l["GroupId"]?.Value<int?>()
                                    ?? 0;
                        return lGroupId == groupId;
                    }));
                }
                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    var userLocationIds = RoleHelper.GetLocationIds(HttpContext);
                    if (userLocationIds.Any())
                    {
                        locations = new Newtonsoft.Json.Linq.JArray(locations.Where(l =>
                        {
                            var id = l["id"]?.Value<int>() ?? l["Id"]?.Value<int>() ?? 0;
                            return userLocationIds.Contains(id);
                        }));
                    }
                }

                return Content(locations.ToString(Newtonsoft.Json.Formatting.None), "application/json");
            }
            return Json(new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationsByCompany(int companyId)
        {
            if (!RoleHelper.ValidateScope(HttpContext, companyId))
                return Json(new { success = false, message = "Access Denied" });

            var locations = new List<Locations>();
            try
            {
                using var client = GetClient();
                var query = BuildScopedQuery(companyId);
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/LocationdataByCompany{query}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<Locations>>(json);
                    if (data != null) locations.AddRange(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetLocationsByCompany Error: {ex.Message}");
            }
            return Json(locations);
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            var locations = new List<Locations>();
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/AllLocations");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<Locations>>(json);
                    if (data != null) locations.AddRange(data);
                }

                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    var userLocationIds = RoleHelper.GetLocationIds(HttpContext);
                    if (userLocationIds.Any())
                    {
                        locations = locations.Where(l => userLocationIds.Contains(l.Id)).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetLocations Error: {ex.Message}");
            }
            return Json(locations);
        }


        public async Task<IActionResult> CheckCompanyName(string name)
        {
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Companycheck?Name={name}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content)
                        ? JsonConvert.DeserializeObject<List<Companies>>(content)
                        : new List<Companies>();

                    bool exists = data != null && data.Any(c => c.CompanyName.Equals(name, StringComparison.OrdinalIgnoreCase));
                    return Ok(new { exists });
                }
                return Ok(new { exists = false });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CheckCompanyName Error: {ex.Message}");
                return Ok(new { exists = false });
            }
        }

        [DynamicPermission("Companies.Create", "Add Company")]
        public async Task<IActionResult> CompanyAdd([FromBody] Companies company)
        {
            if (!RoleHelper.HasPermission(HttpContext, "Companies.Create"))
                return Json(new { status = "error", message = "Access Denied: Missing Companies.Create permission." });

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(company);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/Companyadd", content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                    return Json(jsonResponse);
                }
                return Json(new { status = "error", message = "Failed to add company" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CompanyAdd Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        [DynamicPermission("Companies.Edit", "Update Company")]
        public async Task<IActionResult> UpdateCompany([FromBody] Companies company)
        {
            if (!RoleHelper.ValidateScope(HttpContext, company.Id))
                return Json(new { status = "error", message = "Access Denied: You cannot modify this company." });

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(company);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/Companyupdate", content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                    return Json(jsonResponse);
                }
                return Json(new { status = "error", message = "Failed to update company" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateCompany Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logo(string path)
        {
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}{path}");
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/png";
                    return File(bytes, contentType);
                }
            }
            catch { }
            return NotFound();
        }

        [HttpPost]
        [DynamicPermission("Companies.Edit", "Upload Logo")]
        public async Task<IActionResult> UploadLogo(IFormFile logo, int companyId)
        {
            if (!RoleHelper.ValidateScope(HttpContext, companyId))
                return Json(new { status = "error", message = "Access Denied: You cannot modify this company." });

            if (logo == null || logo.Length == 0)
                return Json(new { status = "error", message = "No file uploaded." });

            try
            {
                using var client = GetClient();
                using var content = new MultipartFormDataContent();

                using var stream = logo.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(logo.ContentType);

                content.Add(fileContent, "logo", logo.FileName);
                content.Add(new StringContent(companyId.ToString()), "companyId");

                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/LogoUpload", content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                    return Json(jsonResponse);
                }
                return Json(new { status = "error", message = "Failed to upload logo" });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UploadLogo Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [DynamicPermission("Companies.View", "View Groups")]
        public async Task<IActionResult> GroupsDetails(int id, string companyName)
        {
            if (!RoleHelper.ValidateScope(HttpContext, id))
                return RedirectToAction("AccessDenied", "Auth");

            var activeComputers = new List<string>();
            var remoteSessions = 0;
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Command/GetConnectedDevices");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var clients = JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content);
                    if (clients != null)
                    {
                        activeComputers = clients
                            .Select(c => c.UserName?.Trim().ToUpper() ?? "")
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            ViewBag.ActiveComputers = activeComputers;
            ViewBag.RemoteSessions = remoteSessions;

            var company = await LoadSingleCompanyHierarchyAsync(id, companyName);
            ViewBag.ApiBaseUrl = _baseUrl;

            return View(company);
        }



        [HttpPost]
        [DynamicPermission("Companies.Create", "Add Group")]
        public async Task<IActionResult> GroupAdd([FromBody] Groups groups)
        {
            if (!RoleHelper.ValidateScope(HttpContext, groups.CompanyID))
                return Json(new { status = "error", message = "Access Denied: You cannot add groups to this company." });

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(groups);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/Groupadd", content);
                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GroupAdd Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Groupupdate([FromBody] Groups groups)
        {
            if (!RoleHelper.ValidateScope(HttpContext, groups.CompanyID))
                return Json(new { status = "error", message = "Access Denied" });

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(groups);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/Groupupdate", content);
                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Groupupdate Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }


        [DynamicPermission("Companies.View", "View Locations")]
        public async Task<IActionResult> LocationDetails(int id, int ComId, string Groupname, string companyname)
        {
            if (!RoleHelper.ValidateScope(HttpContext, ComId, id))
                return RedirectToAction("AccessDenied", "Auth");

            ViewBag.groupId = id;
            ViewBag.CompanyId = ComId;
            ViewBag.CompanyName = companyname;
            ViewBag.groupName = Groupname;

            var data = new List<Locations>();
            try
            {
                using var client = GetClient();
                var query = BuildScopedQuery(ComId, null, id);
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata{query}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Locations>>(content) : null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocationDetails Error: {ex.Message}");
            }
            return View(data ?? new List<Locations>());
        }

        [HttpPost]
        [DynamicPermission("Companies.Create", "Add Location")]
        public async Task<IActionResult> LocationAdd([FromBody] Locations locations)
        {
            if (!RoleHelper.ValidateScope(HttpContext, locations.CompanyID, locations.GroupsID))
                return Json(new { status = "error", message = "Access Denied: You cannot add locations here." });

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(locations);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/Locationadd", content);
                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocationAdd Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Locationupdate([FromBody] Locations locations)
        {
            if (!RoleHelper.ValidateScope(HttpContext, locations.CompanyID, locations.GroupsID))
                return Json(new { status = "error", message = "Access Denied" });

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(locations);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/CompaniesDetails/Locationupdate", content);
                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Locationupdate Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Dwonload(int locationId, int groupid, int comId)
        {
            if (!RoleHelper.ValidateScope(HttpContext, comId, groupid, locationId))
                return Json(new { status = "error", message = "Access Denied" });

            var installer = new InstallerRequest
            {
                LocationId = locationId,
                GroupId = groupid,
                CompanyId = comId
            };

            try
            {
                using var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(installer);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_baseUrl}/api/ClientSetup/softwareinstaller", content);
                string result = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject<object>(result);

                if (jsonResponse != null && jsonResponse.success == true && jsonResponse.downloadUrl != null)
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "installersoftware", "Output", "setup.exe");
                    if (System.IO.File.Exists(physicalPath))
                    {
                        byte[] fileBytes = System.IO.File.ReadAllBytes(physicalPath);
                        return File(fileBytes, "application/octet-stream", "setup.exe");
                    }
                    return NotFound("File not found.");
                }
                return Json(jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Download Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }
    }
}