using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class WindowsServiceController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public WindowsServiceController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
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

        public async Task<IActionResult> Index()
        {
            var query = BuildScopedQuery();
            var response = await GetClient().GetAsync($"{_baseUrl}/api/WindowsService{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<WindowsService> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsService>>(content) : null;
                return View(data ?? new List<WindowsService>());
            }

            throw new Exception("Unable to fetch data from the API.");
        }
    }
}
