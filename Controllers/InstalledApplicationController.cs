using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class InstalledApplicationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public InstalledApplicationController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

                private HttpClient GetClient() 
        {
            var client = _httpClientFactory.CreateClient("ManageEngineApi");
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token)) { client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); }
            return client;
        }

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

        public async Task<IActionResult> Index()
        {
            var query = BuildScopedQuery();
            var response = await GetClient().GetAsync($"{_baseUrl}/api/InstalledApplication{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<InstalledApplication> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                return View(data ?? new List<InstalledApplication>());
            }

            throw new Exception("Unable to fetch data from the API.");
        }
    }
}
