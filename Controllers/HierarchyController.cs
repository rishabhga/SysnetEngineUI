using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class HierarchyController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public HierarchyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431";
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");

        public async Task<IActionResult> Index(string searchTerm = "", string filterCompany = "all", string filterLicenseStatus = "all")
        {
            try
            {
                var hierarchyData = await LoadHierarchyData();
                var filteredData = ApplyFilters(hierarchyData ?? new List<CompanyHierarchyDto>(), searchTerm, filterCompany, filterLicenseStatus);

                ViewBag.SearchTerm = searchTerm ?? string.Empty;
                ViewBag.FilterCompany = filterCompany ?? "all";
                ViewBag.FilterLicenseStatus = filterLicenseStatus ?? "all";

                return View(filteredData);
            }
            catch (Exception)
            {
                return View(new List<CompanyHierarchyDto>());
            }
        }

        public async Task<IActionResult> GraphView()
        {
            try
            {
                var hierarchyData = await LoadHierarchyData();
                return View(hierarchyData ?? new List<CompanyHierarchyDto>());
            }
            catch (Exception)
            {
                return View(new List<CompanyHierarchyDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHierarchyData()
        {
            try
            {
                var hierarchyData = await LoadHierarchyData();
                return Json(hierarchyData ?? new List<CompanyHierarchyDto>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<List<CompanyHierarchyDto>> LoadHierarchyData()
        {
            using var httpClient = GetClient();
            var hierarchyList = new List<CompanyHierarchyDto>();

            try
            {
                var companiesResponse = await httpClient.GetAsync($"{_baseUrl}/api/CompaniesDetails/Companiesdata");

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

                // Scope filter: restrict to user's assigned company
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

                    var groupsResponse = await httpClient.GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata?id={company.Id}");

                    if (groupsResponse.IsSuccessStatusCode)
                    {
                        var groupsJson = await groupsResponse.Content.ReadAsStringAsync();
                        var groups = JsonConvert.DeserializeObject<List<Groups>>(groupsJson);

                        if (groups != null)
                        {
                            // Scope filter: restrict to user's assigned group
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

                                var locationsResponse = await httpClient.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata?comid={company.Id}&groupid={group.Id}");

                                if (locationsResponse.IsSuccessStatusCode)
                                {
                                    var locationsJson = await locationsResponse.Content.ReadAsStringAsync();
                                    var locations = JsonConvert.DeserializeObject<List<Locations>>(locationsJson);

                                    if (locations != null)
                                    {
                                        // Scope filter: restrict to user's assigned location
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

                                            var usersResponse = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser?locationId={location.Id}&groupid={group.Id}&comId={company.Id}");

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

        private List<CompanyHierarchyDto> ApplyFilters(List<CompanyHierarchyDto> companies, string searchTerm, string filterCompany, string filterLicenseStatus)
        {
            var filtered = companies.ToList();

            if (!string.IsNullOrEmpty(filterCompany) && filterCompany != "all")
            {
                if (int.TryParse(filterCompany, out int companyId))
                {
                    filtered = filtered.Where(c => c.CompanyId == companyId).ToList();
                }
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                filtered = filtered.Select(company => new CompanyHierarchyDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    Groups = company.Groups.Select(group => new GroupHierarchyDto
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        Locations = group.Locations.Select(location => new LocationHierarchyDto
                        {
                            LocationId = location.LocationId,
                            LocationName = location.LocationName,
                            Users = location.Users.Where(user =>
                                (user.UserName?.ToLower().Contains(searchLower) ?? false) ||
                                (user.DomainName?.ToLower().Contains(searchLower) ?? false) ||
                                (user.IpAddress?.ToLower().Contains(searchLower) ?? false) ||
                                (location.LocationName?.ToLower().Contains(searchLower) ?? false)
                            ).ToList()
                        }).Where(l => l.Users.Any()).ToList()
                    }).Where(g => g.Locations.Any()).ToList()
                }).Where(c => c.Groups.Any()).ToList();
            }

            if (!string.IsNullOrEmpty(filterLicenseStatus) && filterLicenseStatus != "all")
            {
                filtered = filtered.Select(company => new CompanyHierarchyDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    Groups = company.Groups.Select(group => new GroupHierarchyDto
                    {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        Locations = group.Locations.Select(location => new LocationHierarchyDto
                        {
                            LocationId = location.LocationId,
                            LocationName = location.LocationName,
                            Users = location.Users.Where(u =>
                                u.OsLicenseStatus?.Equals(filterLicenseStatus, StringComparison.OrdinalIgnoreCase) == true
                            ).ToList()
                        }).Where(l => l.Users.Any()).ToList()
                    }).Where(g => g.Locations.Any()).ToList()
                }).Where(c => c.Groups.Any()).ToList();
            }

            return filtered;
        }
    }
}
