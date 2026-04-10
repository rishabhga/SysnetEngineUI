using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace ManageEngineWebApp.Controllers
{

    [AuthFilter]
    public class CompaniesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public CompaniesController(IHttpClientFactory httpClientFactory, IWebHostEnvironment env, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }
        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");
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

            var hierarchyData = await LoadHierarchyData();
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
            var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata?id={companyId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            return Json(new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationsByGroup(int companyId, int groupId)
        {
            if (!RoleHelper.ValidateScope(HttpContext, companyId, groupId))
                return Json(new { success = false, message = "Access Denied" });

            using var client = GetClient();
            var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata?comid={companyId}&groupid={groupId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            return Json(new List<object>());
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationsByCompany(int companyId)
        {
            // Scope check
            if (!RoleHelper.ValidateScope(HttpContext, companyId))
                return Json(new { success = false, message = "Access Denied" });

            var locations = new List<Locations>();
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/LocationdataByCompany?comid={companyId}");
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

        private async Task<List<CompanyHierarchyDto>> LoadHierarchyData()
        {
            var hierarchyList = new List<CompanyHierarchyDto>();

            try
            {
                using var client = GetClient();

                var companiesResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Companiesdata");
                if (!companiesResponse.IsSuccessStatusCode)
                    return hierarchyList;

                var companiesJson = await companiesResponse.Content.ReadAsStringAsync();
                var companies = JsonConvert.DeserializeObject<List<Companies>>(companiesJson);

                if (companies == null || !companies.Any())
                    return hierarchyList;

                if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                {
                    var allowedCompanyIds = RoleHelper.GetCompanyIds(HttpContext);
                    companies = companies.Where(c => allowedCompanyIds.Contains(c.Id)).ToList();
                }

                foreach (var company in companies)
                {
                    var companyDto = new CompanyHierarchyDto
                    {
                        CompanyId = company.Id,
                        CompanyName = company.CompanyName ?? "Unknown",
                        Groups = new List<GroupHierarchyDto>()
                    };

                    var groupsResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata?id={company.Id}");
                    if (groupsResponse.IsSuccessStatusCode)
                    {
                        var groupsJson = await groupsResponse.Content.ReadAsStringAsync();
                        var groups = JsonConvert.DeserializeObject<List<Groups>>(groupsJson);

                        if (groups != null)
                        {
                            if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                            {
                                var allowedGroupIds = RoleHelper.GetGroupIds(HttpContext);
                                groups = groups.Where(g => allowedGroupIds.Contains(g.Id)).ToList();
                            }

                            foreach (var group in groups)
                            {
                                var groupDto = new GroupHierarchyDto
                                {
                                    GroupId = group.Id,
                                    GroupName = group.GroupName ?? "Unknown",
                                    Locations = new List<LocationHierarchyDto>()
                                };

                                var locationsResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata?comid={company.Id}&groupid={group.Id}");
                                if (locationsResponse.IsSuccessStatusCode)
                                {
                                    var locationsJson = await locationsResponse.Content.ReadAsStringAsync();
                                    var locations = JsonConvert.DeserializeObject<List<Locations>>(locationsJson);

                                    if (locations != null)
                                    {
                                        if (!RoleHelper.IsTopLevelAdmin(HttpContext))
                                        {
                                            var allowedLocationIds = RoleHelper.GetLocationIds(HttpContext);
                                            locations = locations.Where(l => allowedLocationIds.Contains(l.Id)).ToList();
                                        }

                                        foreach (var location in locations)
                                        {
                                            var locationDto = new LocationHierarchyDto
                                            {
                                                LocationId = location.Id,
                                                LocationName = location.LocationName ?? "Unknown",
                                                IsCritical = location.IsCritical,
                                                Users = new List<UserHierarchyDto>()
                                            };

                                            var usersResponse = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser?locationId={location.Id}&groupid={group.Id}&comId={company.Id}");
                                            if (usersResponse.IsSuccessStatusCode)
                                            {
                                                var usersJson = await usersResponse.Content.ReadAsStringAsync();
                                                var users = JsonConvert.DeserializeObject<List<UserDetails>>(usersJson);

                                                if (users != null && users.Any())
                                                {
                                                    foreach (var user in users)
                                                    {
                                                        locationDto.Users.Add(new UserHierarchyDto
                                                        {
                                                            UserName = user.UserName ?? "Unknown",
                                                            IpAddress = user.IpAddress ?? "N/A",
                                                            OsLicenseStatus = user.OsLicenseStatus ?? "Unknown",
                                                            DomainName = user.domainName ?? "N/A",
                                                            PrimaryOwner = user.PrimaryOwner ?? "N/A"
                                                        });
                                                    }
                                                }
                                            }
                                            groupDto.Locations.Add(locationDto);
                                        }
                                    }
                                }
                                companyDto.Groups.Add(groupDto);
                            }
                        }
                    }
                    hierarchyList.Add(companyDto);
                }
                return hierarchyList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadHierarchyData Error: {ex.Message}");
                return hierarchyList;
            }
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

            var company = await LoadSingleCompanyHierarchy(id, companyName);
            return View(company);
        }


        private async Task<CompanyHierarchyDto> LoadSingleCompanyHierarchy(int companyId, string companyName)
        {
            using var client = GetClient();

            var companyResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/CompanyById?id={companyId}");
            var companyJson = await companyResponse.Content.ReadAsStringAsync();
            var company = !string.IsNullOrEmpty(companyJson) ? JsonConvert.DeserializeObject<Companies>(companyJson) : null;

            var companyDto = new CompanyHierarchyDto
            {
                CompanyId = company?.Id ?? companyId,
                CompanyName = company?.CompanyName ?? companyName ?? "Unknown",
                Groups = new List<GroupHierarchyDto>()
            };

            var groupsResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata?id={companyId}");
            var groupsJson = await groupsResponse.Content.ReadAsStringAsync();
            var groups = !string.IsNullOrEmpty(groupsJson) ? JsonConvert.DeserializeObject<List<Groups>>(groupsJson) : null;

            if (groups != null)
            {
                var allowedGroupIds = RoleHelper.GetGroupIds(HttpContext);
                if (allowedGroupIds.Any())
                {
                    groups = groups.Where(g => allowedGroupIds.Contains(g.Id)).ToList();
                }

                foreach (var group in groups)
                {
                    var groupDto = new GroupHierarchyDto
                    {
                        GroupId = group.Id,
                        GroupName = group.GroupName ?? "Unknown",
                        Locations = new List<LocationHierarchyDto>()
                    };

                    var locationsResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata?comid={companyId}&groupid={group.Id}");
                    var locationsJson = await locationsResponse.Content.ReadAsStringAsync();
                    var locations = !string.IsNullOrEmpty(locationsJson) ? JsonConvert.DeserializeObject<List<Locations>>(locationsJson) : null;

                    if (locations != null)
                    {
                        var allowedLocationIds = RoleHelper.GetLocationIds(HttpContext);
                        if (allowedLocationIds.Any())
                        {
                            locations = locations.Where(l => allowedLocationIds.Contains(l.Id)).ToList();
                        }

                        foreach (var location in locations)
                        {
                            var locationDto = new LocationHierarchyDto
                            {
                                LocationId = location.Id,
                                LocationName = location.LocationName ?? "Unknown",
                                IsCritical = location.IsCritical,
                                Users = new List<UserHierarchyDto>()
                            };

                            var usersResponse = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser?locationId={location.Id}&groupid={group.Id}&comId={companyId}");
                            var usersJson = await usersResponse.Content.ReadAsStringAsync();
                            var users = !string.IsNullOrEmpty(usersJson) ? JsonConvert.DeserializeObject<List<UserDetails>>(usersJson) : null;

                            if (users != null)
                            {
                                foreach (var user in users)
                                {
                                    locationDto.Users.Add(new UserHierarchyDto
                                    {
                                        UserName = user.UserName ?? "Unknown",
                                        IpAddress = user.IpAddress ?? "N/A",
                                        OsLicenseStatus = user.OsLicenseStatus ?? "Unknown",
                                        DomainName = user.domainName ?? "N/A",
                                        PrimaryOwner = user.PrimaryOwner ?? "N/A"
                                    });
                                }
                            }
                            groupDto.Locations.Add(locationDto);
                        }
                    }
                    companyDto.Groups.Add(groupDto);
                }
            }
            return companyDto;
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
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata?comid={ComId}&groupid={id}");
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
