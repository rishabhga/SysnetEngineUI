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
    public class USBHubDetailsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public USBHubDetailsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431";
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
            var response = await GetClient().GetAsync($"{_baseUrl}/api/USBHubDetails{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<USBHubDetails> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBHubDetails>>(content) : null;
                return View(data ?? new List<USBHubDetails>());
            }

            throw new Exception("Unable to fetch data from the API.");
        }
    }
}
