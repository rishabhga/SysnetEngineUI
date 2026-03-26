using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using ManageEngineWebApp.Datacontext;
using Newtonsoft.Json;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Controllers
{
    public class WindowsUserDetailsController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public WindowsUserDetailsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
            //_baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431";
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");

        private (List<int> companyIds, List<int> groupIds, List<int> locationIds) GetUserScope()
        {
            if (RoleHelper.IsTopLevelAdmin(HttpContext)) return (new List<int>(), new List<int>(), new List<int>());
            return (RoleHelper.GetCompanyIds(HttpContext), 
                    RoleHelper.GetGroupIds(HttpContext), 
                    RoleHelper.GetLocationIds(HttpContext));
        }

        private string BuildScopedQuery()
        {
            var (userCompanyIds, userGroupIds, userLocationIds) = GetUserScope();
            var q = new List<string>();
            foreach (var id in userCompanyIds) q.Add($"companyId={id}");
            foreach (var id in userLocationIds) q.Add($"locationId={id}");
            foreach (var id in userGroupIds) q.Add($"groupId={id}");
            return q.Any() ? "?" + string.Join("&", q) : "";
        }

        private async Task<bool> IsDeviceAuthorized(string domainOrUserCode)
        {
            if (RoleHelper.IsTopLevelAdmin(HttpContext)) return true;
            using var client = GetClient();
            var response = await client.GetAsync("api/WindowsUserDetails");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content);
                var machine = data?.FirstOrDefault(x => x.DomainName == domainOrUserCode || x.UserCode == domainOrUserCode);
                if (machine != null)
                {
                    return RoleHelper.ValidateScope(HttpContext, machine.CompanyId, machine.GroupId, machine.LocationId);
                }
            }
            return false;
        }

        [AuthFilter]
        public async Task<IActionResult> Index(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return RedirectToAction("Index", "Home");

            using var client = GetClient();
            var response = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                var datalist = data != null ? data.Where(x => x != null && x.DomainName == domain).ToList() : new List<WindowsUserDetails>();
                return View(datalist);
            }

            throw new Exception("Unable to fetch data from the API.");
        }

        [AuthFilter]
        public async Task<IActionResult> Userview()
        {
            using var client = GetClient();
            var query = BuildScopedQuery();
            var response = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                var datalist = data != null ? data.Where(x => x != null && x.Status == "Enabled").ToList() : new List<WindowsUserDetails>();
                return View(datalist);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //public IActionResult Index()
        //{
        //    return View();
        //}
    }
}
