using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ManageEngineWebApp.Controllers
{

    [AuthFilter]
    public class CompaniesController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;

        public CompaniesController(HttpClient httpClient, IWebHostEnvironment env)
        {


            _httpClient = httpClient;
            _env = env;
        }
        public async Task<IActionResult> Companies()
        {
            var username = HttpContext.Session.GetString("username");
            if (RoleHelper.IsCompanyAdmin(HttpContext))
            {
                var companyId = RoleHelper.GetCompanyId(HttpContext);
                if (companyId.HasValue)
                {
                    return RedirectToAction("GroupsDetails", new { id = companyId.Value });
                }
            }

            // Fetch active computers from SignalR
            var activeComputers = new List<string>();
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    var response = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices");
                    //var response = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices");
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
                            System.Diagnostics.Debug.WriteLine($"Active Computers Count: {activeComputers.Count}");
                            System.Diagnostics.Debug.WriteLine($"Active Computers: {string.Join(", ", activeComputers.Take(5))}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching connected devices: {ex.Message}");
                }
            }
            ViewBag.ActiveComputers = activeComputers;

            var hierarchyData = await LoadHierarchyData();
            return View(hierarchyData ?? new List<CompanyHierarchyDto>());
        }
        [HttpGet]
        public async Task<IActionResult> GetCompaniesForDropdown()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<Companies>();
            using (var httpClient = new HttpClient(handler))
            {
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companiesdata");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companiesdata");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Companies>>(content) : new List<Companies>();
                }

                return Json(data);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupsByCompany(int companyId)
        {
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            using var client = new HttpClient(handler);
            var response = await client.GetAsync($"https://localhost:7225/api/CompaniesDetails/Groupdata?id={companyId}");
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
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (m, c, ch, e) => true };
            using var client = new HttpClient(handler);
            var response = await client.GetAsync($"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={companyId}&groupid={groupId}");
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var locations = new List<Locations>();
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    // Get all locations for this company via API (Unified endpoint)
                    var locationsUrl = $"https://localhost:7225/api/CompaniesDetails/LocationdataByCompany?comid={companyId}";
                    var response = await httpClient.GetAsync(locationsUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<Locations>>(json);
                        if (data != null) locations.AddRange(data);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetLocationsByCompany Error: {ex.Message}");
                }
                return Json(locations);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            // Keeping original GetLocations for other parts of app if needed, 
            // but ensuring it uses localhost
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var locations = new List<Locations>();
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    var response = await httpClient.GetAsync("https://localhost:7225/api/CompaniesDetails/AllLocations");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<List<Locations>>(json);
                        if (data != null) locations.AddRange(data);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetLocations Error: {ex.Message}");
                }
                return Json(locations);
            }
        }


        private async Task<List<CompanyHierarchyDto>> LoadHierarchyData()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            var hierarchyList = new List<CompanyHierarchyDto>();

            try
            {
                var companiesUrl = "https://localhost:7225/api/CompaniesDetails/Companiesdata";
                //var companiesUrl = "https://localhost:7225/api/CompaniesDetails/Companiesdata";
                var companiesResponse = await httpClient.GetAsync(companiesUrl);

                if (!companiesResponse.IsSuccessStatusCode)
                {
                    return hierarchyList;
                }

                var companiesJson = await companiesResponse.Content.ReadAsStringAsync();
                var companies = JsonConvert.DeserializeObject<List<Companies>>(companiesJson);

                if (companies == null || !companies.Any())
                {
                    return hierarchyList;
                }

                int? allowedCompanyId = RoleHelper.GetCompanyId(HttpContext);
                if (allowedCompanyId.HasValue)
                {
                    companies = companies.Where(c => c.Id == allowedCompanyId.Value).ToList();
                }

                foreach (var company in companies)
                {
                    var companyDto = new CompanyHierarchyDto
                    {
                        CompanyId = company.Id,
                        CompanyName = company.CompanyName ?? "Unknown",
                        Groups = new List<GroupHierarchyDto>()
                    };

                    //var groupsUrl = $"https://localhost:7225/api/CompaniesDetails/Groupdata?id={company.Id}";
                    var groupsUrl = $"https://localhost:7225/api/CompaniesDetails/Groupdata?id={company.Id}";

                    var groupsResponse = await httpClient.GetAsync(groupsUrl);

                    if (groupsResponse.IsSuccessStatusCode)
                    {
                        var groupsJson = await groupsResponse.Content.ReadAsStringAsync();
                        var groups = JsonConvert.DeserializeObject<List<Groups>>(groupsJson);

                        if (groups != null)
                        {
                            int? allowedGroupId = RoleHelper.GetGroupId(HttpContext);
                            if (allowedGroupId.HasValue)
                            {
                                groups = groups.Where(g => g.Id == allowedGroupId.Value).ToList();
                            }

                            foreach (var group in groups)
                            {
                                var groupDto = new GroupHierarchyDto
                                {
                                    GroupId = group.Id,
                                    GroupName = group.GroupName ?? "Unknown",
                                    Locations = new List<LocationHierarchyDto>()
                                };

                                var locationsUrl = $"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={company.Id}&groupid={group.Id}";
                                //var locationsUrl = $"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={company.Id}&groupid={group.Id}";
                                var locationsResponse = await httpClient.GetAsync(locationsUrl);

                                if (locationsResponse.IsSuccessStatusCode)
                                {
                                    var locationsJson = await locationsResponse.Content.ReadAsStringAsync();
                                    var locations = JsonConvert.DeserializeObject<List<Locations>>(locationsJson);

                                    if (locations != null)
                                    {
                                        int? allowedLocationId = RoleHelper.GetLocationId(HttpContext);
                                        if (allowedLocationId.HasValue)
                                        {
                                            locations = locations.Where(l => l.Id == allowedLocationId.Value).ToList();
                                        }

                                        foreach (var location in locations)
                                        {
                                            var locationDto = new LocationHierarchyDto
                                            {
                                                LocationId = location.Id,
                                                LocationName = location.LocationName ?? "Unknown",
                                                Users = new List<UserHierarchyDto>()
                                            };

                                            var usersUrl = $"https://localhost:7225/api/WindowsUserDetails/allUser?locationId={location.Id}&&groupid={group.Id}&&comId={company.Id}";
                                            var usersResponse = await httpClient.GetAsync(usersUrl);

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
                System.Diagnostics.Debug.WriteLine($"LoadHierarchyData Error: {ex.Message}");
                return hierarchyList;
            }
        }

        //public async Task<IActionResult> CheckCompanyName(string name)
        //{
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    var data = new List<Companies>();
        //    bool exists;

        //    using (var httpClient = new HttpClient(handler))
        //    {
        //        httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Companycheck?Name={name}");
        //        //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Companycheck?Name={name}");
        //        //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Companycheck?Name={name}");

        //        // Send POST request to the server
        //        var response = await httpClient.GetAsync("");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Companies>>(content) : null;
        //            //return View(data);

        //        }
        //        if (data != null)
        //        {
        //            exists = data.Any(c => c.CompanyName.ToLower() == name.ToLower());
        //        }
        //        else
        //        {
        //            exists = false;
        //        }
        //        return Ok(new { exists });
        //    }
        //}
        //  company Add
        //public async Task<IActionResult> CompanyAdd([FromBody] Companies company)
        //{
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };         


        //    using (HttpClient client = new HttpClient(handler))
        //    {

        //       // client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyadd");
        //        client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyadd");
        //        string jsonData = JsonConvert.SerializeObject(company);
        //        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



        //        HttpResponseMessage response = await client.PostAsync("", content);

        //        string result = await response.Content.ReadAsStringAsync();
        //        var jsonResponse = JsonConvert.DeserializeObject<object>(result);
        //        return Json(jsonResponse);
        //        //return Json(result);
        //       // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
        //    }



        //}

        ////UpdateCompany
        //[HttpPost]
        //public async Task<IActionResult> UpdateCompany([FromBody] Companies company)
        //{
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };


        //    using (HttpClient client = new HttpClient(handler))
        //    {

        //        //client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyupdate");
        //        client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyupdate");
        //        string jsonData = JsonConvert.SerializeObject(company);
        //        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



        //        HttpResponseMessage response = await client.PostAsync("", content);

        //        string result = await response.Content.ReadAsStringAsync();
        //        var jsonResponse = JsonConvert.DeserializeObject<object>(result);
        //        return Json(jsonResponse);
        //        //return Json(result);
        //        // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
        //    }
        //}


        // Rishabh Gaur


        public async Task<IActionResult> CheckCompanyName(string name)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Companycheck?Name={name}");
                //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Companycheck?Name={name}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content)
                        ? JsonConvert.DeserializeObject<List<Companies>>(content)
                        : new List<Companies>();

                    bool exists = data != null && data.Any(c => c.CompanyName.Equals(name, StringComparison.OrdinalIgnoreCase));

                    return Ok(new { exists = exists });
                }

                return Ok(new { exists = false });
            }
        }

        public async Task<IActionResult> CompanyAdd([FromBody] Companies company)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyadd");
                    //client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyadd");
                    string jsonData = JsonConvert.SerializeObject(company);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("", content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                        return Json(jsonResponse);
                    }
                    else
                    {
                        return Json(new { status = "error", message = "Failed to add company" });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompanyAdd Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }

        // UpdateCompany
        [HttpPost]
        public async Task<IActionResult> UpdateCompany([FromBody] Companies company)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyupdate");
                    //client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Companyupdate");
                    string jsonData = JsonConvert.SerializeObject(company);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync("", content);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                        return Json(jsonResponse);
                    }
                    else
                    {
                        return Json(new { status = "error", message = "Failed to update company" });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCompany Error: {ex.Message}");
                return Json(new { status = "error", message = ex.Message });
            }
        }
        public async Task<IActionResult> GroupsDetails(int id,string companyName)
        {
            var username = HttpContext.Session.GetString("username");
            
            // Fetch active computers from SignalR
            var activeComputers = new List<string>();
            var remoteSessions = 0;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    //var response = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices");
                    var response = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices");
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
                            System.Diagnostics.Debug.WriteLine($"GroupsDetails - Active Computers Count: {activeComputers.Count}");
                            System.Diagnostics.Debug.WriteLine($"GroupsDetails - Sample Computers: {string.Join(", ", activeComputers.Take(5))}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching connected devices: {ex.Message}");
                }
            }
            ViewBag.ActiveComputers = activeComputers;
            ViewBag.RemoteSessions = remoteSessions;

            var company = await LoadSingleCompanyHierarchy(id, companyName);
            return View(company);
        }


        private async Task<CompanyHierarchyDto> LoadSingleCompanyHierarchy(int companyId, string companyName)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            // Get the company info
            //var companyResponse = await httpClient.GetAsync($"https://localhost:7225/api/CompaniesDetails/CompanyById?id={companyId}");
            var companyResponse = await httpClient.GetAsync($"https://localhost:7225/api/CompaniesDetails/CompanyById?id={companyId}");
            var companyJson = await companyResponse.Content.ReadAsStringAsync();
            var company = !string.IsNullOrEmpty(companyJson) ? JsonConvert.DeserializeObject<Companies>(companyJson) : null;

            var companyDto = new CompanyHierarchyDto
            {
                CompanyId = company?.Id ?? companyId,
                CompanyName = company?.CompanyName ?? companyName ?? "Unknown",
                Groups = new List<GroupHierarchyDto>()
            };

            // Get groups for company
            var groupsResponse = await httpClient.GetAsync($"https://localhost:7225/api/CompaniesDetails/Groupdata?id={companyId}");
            //var groupsResponse = await httpClient.GetAsync($"https://localhost:7225/api/CompaniesDetails/Groupdata?id={companyId}");
            var groupsJson = await groupsResponse.Content.ReadAsStringAsync();
            var groups = !string.IsNullOrEmpty(groupsJson) ? JsonConvert.DeserializeObject<List<Groups>>(groupsJson) : null;

            if (groups != null)
            {
                foreach (var group in groups)
                {
                    var groupDto = new GroupHierarchyDto
                    {
                        GroupId = group.Id,
                        GroupName = group.GroupName ?? "Unknown",
                        Locations = new List<LocationHierarchyDto>()
                    };

                    // Get locations for group
                    //var locationsResponse = await httpClient.GetAsync($"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={companyId}&groupid={group.Id}");
                    var locationsResponse = await httpClient.GetAsync($"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={companyId}&groupid={group.Id}");
                    var locationsJson = await locationsResponse.Content.ReadAsStringAsync();
                    var locations = !string.IsNullOrEmpty(locationsJson) ? JsonConvert.DeserializeObject<List<Locations>>(locationsJson) : null;

                    if (locations != null)
                    {
                        foreach (var location in locations)
                        {
                            var locationDto = new LocationHierarchyDto
                            {
                                LocationId = location.Id,
                                LocationName = location.LocationName ?? "Unknown",
                                Users = new List<UserHierarchyDto>()
                            };

                            // Get users for location
                            var usersResponse = await httpClient.GetAsync($"https://localhost:7225/api/WindowsUserDetails/allUser?locationId={location.Id}&&groupid={group.Id}&&comId={companyId}");
                            //var usersResponse = await httpClient.GetAsync($"https://localhost:7225/api/WindowsUserDetails/allUser?locationId={location.Id}&&groupid={group.Id}&&comId={companyId}");
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

        //  Group Add
        [HttpPost]
        public async Task<IActionResult> GroupAdd([FromBody] Groups groups)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (HttpClient client = new HttpClient(handler))
            {

                client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Groupadd");
                //client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Groupadd");
                string jsonData = JsonConvert.SerializeObject(groups);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                //return Json(result);
                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }



        }

        //UpdateGroup

        [HttpPost]
        public async Task<IActionResult> Groupupdate([FromBody] Groups groups)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (HttpClient client = new HttpClient(handler))
            {

                //client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Groupupdate");
                client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Groupupdate");
                    string jsonData = JsonConvert.SerializeObject(groups);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                //return Json(result);
                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }



        }



        public async Task<IActionResult> LocationDetails(int id,int ComId,string Groupname,string companyname)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            ViewBag.groupId = id;
            ViewBag.CompanyId = ComId;
            ViewBag.CompanyName = companyname;
            ViewBag.groupName = Groupname;


            var data = new List<Locations>();

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={ComId}&groupid={id}");
               // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/CompaniesDetails/Locationdata?comid={ComId}&groupid={id}");

                // Send POST request to the server
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Locations>>(content) : null;
                    return View(data);
                }
                return View(data);
            }
           

        }

        //LocationAdd
        [HttpPost]
        public async Task<IActionResult> LocationAdd([FromBody] Locations locations)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (HttpClient client = new HttpClient(handler))
            {

                //client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Locationadd");
                client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Locationadd");
                string jsonData = JsonConvert.SerializeObject(locations);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                //return Json(result);
                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }



        }


        [HttpPost]
        public async Task<IActionResult> Locationupdate([FromBody] Locations locations)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (HttpClient client = new HttpClient(handler))
            {

               // client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Locationupdate");
             client.BaseAddress = new Uri("https://localhost:7225/api/CompaniesDetails/Locationupdate");
                string jsonData = JsonConvert.SerializeObject(locations);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                
            }



        }


        [HttpGet]
        public async Task<IActionResult> Dwonload(int locationId, int groupid, int comId)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var installer = new InstallerRequest
            {
                LocationId = locationId,
                GroupId = groupid,
                CompanyId = comId

            };

            using (HttpClient client = new HttpClient(handler))
            {

               // client.BaseAddress = new Uri("https://localhost:4436/api/ClientSetup/softwareinstaller");
               client.BaseAddress = new Uri("https://localhost:7225/api/ClientSetup/softwareinstaller");
                string jsonData = JsonConvert.SerializeObject(installer);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse =  JsonConvert.DeserializeObject<object>(result);
                if (jsonResponse != null && jsonResponse.success == true && jsonResponse.downloadUrl != null)
                {
                    // Map relative URL to physical file
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "installersoftware", "Output", "setup.exe");

                    if (System.IO.File.Exists(physicalPath))
                    {
                        byte[] fileBytes = System.IO.File.ReadAllBytes(physicalPath);
                        return File(fileBytes, "application/octet-stream", "setup.exe");
                    }
                    else
                    {
                        return NotFound("File not found.");
                    }
                }
                return Json(jsonResponse);





            }



        }



    }
}
