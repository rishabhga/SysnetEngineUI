using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Mvc;
using ManageEngineWebApp.Datacontext;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class WindowDriversController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public WindowDriversController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
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
            var response = await GetClient().GetAsync($"{_baseUrl}/api/WindowDrivers{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<WindowDrivers> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                ViewBag.WindowDrivers = data;
                return View(data ?? new List<WindowDrivers>());
            }

            throw new Exception("Unable to fetch data from the API.");
        }

        public async Task<IActionResult> datalist()
        {
            var query = BuildScopedQuery();
            var response = await GetClient().GetAsync($"{_baseUrl}/api/WindowDrivers{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<WindowDrivers> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                return Json(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }

        public async Task<IActionResult> users()
        {
            var query = BuildScopedQuery();
            var response = await GetClient().GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<WindowsUserDetails> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                return Json(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }

        public async Task<IActionResult> Services()
        {
            var query = BuildScopedQuery();
            var response = await GetClient().GetAsync($"{_baseUrl}/api/WindowDrivers{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                List<WindowDrivers> data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                return Json(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
    }
}
