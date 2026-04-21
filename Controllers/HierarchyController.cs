using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
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
    public class HierarchyController : BaseController
    {
        public HierarchyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
            : base(httpClientFactory, configuration)
        {
        }

        [DynamicPermission("Hierarchy.View", "View Hierarchy List")]
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

        [DynamicPermission("Hierarchy.View", "View Hierarchy Graph")]
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
            return await LoadHierarchyAsync();
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
