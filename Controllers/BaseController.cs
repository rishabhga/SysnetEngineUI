using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.Dtos;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ManageEngineWebApp.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly IConfiguration _configuration;
        protected readonly string _baseUrl;

        public BaseController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _baseUrl = _configuration["ApiSettings:BaseUrl"];
        }

        protected HttpClient GetClient()
        {
            var client = _httpClientFactory.CreateClient("ManageEngineApi");

            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        protected bool IsTopLevelAdmin() => RoleHelper.IsTopLevelAdmin(HttpContext);

        protected bool HasPermission(string code) => RoleHelper.HasPermission(HttpContext, code);

        protected (List<int> companyIds, List<int> groupIds, List<int> locationIds) GetUserScope()
        {
            if (IsTopLevelAdmin()) return (new List<int>(), new List<int>(), new List<int>());
            return (RoleHelper.GetCompanyIds(HttpContext),
                    RoleHelper.GetGroupIds(HttpContext),
                    RoleHelper.GetLocationIds(HttpContext));
        }

        protected bool IsAuthorized(int? comId, int? groupId = null, int? locationId = null)
        {
            return RoleHelper.ValidateScope(HttpContext, comId, groupId, locationId);
        }

        protected async Task<bool> IsDeviceAuthorized(string domainOrUserCode)
        {
            if (IsTopLevelAdmin()) return true;
            try
            {

                var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content);
                    var machine = data?.FirstOrDefault(x =>
                        string.Equals(x.DomainName, domainOrUserCode, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x.UserCode, domainOrUserCode, StringComparison.OrdinalIgnoreCase));
                    if (machine != null)
                    {
                        return RoleHelper.ValidateScope(HttpContext, machine.CompanyId, machine.GroupId, machine.LocationId);
                    }
                }
            }
            catch { }
            return false;
        }
        protected string BuildScopedQuery(int? companyId = null, int? locationId = null, int? groupId = null)
        {
            var (userCompanyIds, userGroupIds, userLocationIds) = GetUserScope();
            var q = new List<string>();
            if (companyId.HasValue && companyId.Value > 0)
            {
                if (IsAuthorized(companyId))
                    q.Add($"companyId={companyId}");
            }
            else if (userCompanyIds.Any())
            {
                foreach (var id in userCompanyIds) q.Add($"companyId={id}");
            }
            if (locationId.HasValue && locationId.Value > 0)
            {
                if (IsAuthorized(null, null, locationId))
                    q.Add($"locationId={locationId}");
            }
            else if (userLocationIds.Any())
            {
                foreach (var id in userLocationIds) q.Add($"locationId={id}");
            }

            if (groupId.HasValue && groupId.Value > 0)
            {
                if (IsAuthorized(null, groupId))
                    q.Add($"groupId={groupId}");
            }
            else if (userGroupIds.Any())
            {
                foreach (var id in userGroupIds) q.Add($"groupId={id}");
            }

            return q.Any() ? "?" + string.Join("&", q) : "";
        }

        protected string GetUCodeFromDomain(string domain)
        {
            return domain;
        }

        protected async Task<List<CompanyHierarchyDto>> LoadHierarchyAsync()
        {
            var hierarchy = new List<CompanyHierarchyDto>();
            try
            {
                var client = GetClient();

                var companiesTask = client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Companiesdata");
                var groupsTask = client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata");
                var locationsTask = client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata");
                var usersTask = client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser");
                var activeDevicesTask = client.GetAsync($"{_baseUrl}/api/Command/GetConnectedDevices");

                await Task.WhenAll(companiesTask, groupsTask, locationsTask, usersTask, activeDevicesTask);

                var companies = JsonConvert.DeserializeObject<List<Companies>>(
                    await companiesTask.Result.Content.ReadAsStringAsync()) ?? new List<Companies>();
                var groups = JsonConvert.DeserializeObject<List<Groups>>(
                    await groupsTask.Result.Content.ReadAsStringAsync()) ?? new List<Groups>();
                var locations = JsonConvert.DeserializeObject<List<Locations>>(
                    await locationsTask.Result.Content.ReadAsStringAsync()) ?? new List<Locations>();
                var users = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(
                    await usersTask.Result.Content.ReadAsStringAsync()) ?? new List<WindowsUserDetails>();
                var activeDevices = JsonConvert.DeserializeObject<List<ConnectedClientDto>>(
                    await activeDevicesTask.Result.Content.ReadAsStringAsync()) ?? new List<ConnectedClientDto>();

                var activeUserNames = activeDevices.Select(d => d.UserName?.ToUpper().Trim()).ToHashSet();

                foreach (var com in companies)
                {
                    var comDto = new CompanyHierarchyDto
                    {
                        CompanyId = com.Id,
                        CompanyName = com.CompanyName,
                        LogoUrl = com.LogoUrl
                    };

                    var comGroups = groups.Where(g => g.CompanyID == com.Id).ToList();
                    foreach (var grp in comGroups)
                    {
                        var grpDto = new GroupHierarchyDto { GroupId = grp.Id, GroupName = grp.GroupName };

                        var grpLocs = locations.Where(l => l.GroupsID == grp.Id).ToList();
                        foreach (var loc in grpLocs)
                        {
                            var locDto = new LocationHierarchyDto 
                            { 
                                LocationId = loc.Id, 
                                LocationName = loc.LocationName,
                                IsCritical = loc.IsCritical
                            };

                            var locUsers = users.Where(u => u.LocationId == loc.Id).ToList();
                            foreach (var usr in locUsers)
                            {
                                locDto.Users.Add(new UserHierarchyDto
                                {
                                    UserName = !string.IsNullOrEmpty(usr.UserName) ? usr.UserName : 
                                               (!string.IsNullOrEmpty(usr.FullName) ? usr.FullName : usr.UserCode),
                                    DomainName = usr.DomainName,
                                    PrimaryOwner = usr.FullName,
                                    OsLicenseStatus = usr.AccountType ?? "PENDING",
                                    IsOnline = activeUserNames.Contains(usr.DomainName?.ToUpper().Trim() ?? "") || 
                                               activeUserNames.Contains(usr.UserCode?.ToUpper().Trim() ?? "")
                                });
                            }
                            grpDto.Locations.Add(locDto);
                        }
                        comDto.Groups.Add(grpDto);
                    }
                    hierarchy.Add(comDto);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hierarchy Load Error: {ex.Message}");
            }
            return hierarchy;
        }

        protected async Task<CompanyHierarchyDto?> LoadSingleCompanyHierarchyAsync(int companyId, string companyName)
        {
            var hierarchy = await LoadHierarchyAsync();
            return hierarchy.FirstOrDefault(c => c.CompanyId == companyId);
        }
    }
}