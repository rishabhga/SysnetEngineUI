using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using ManageEngineWebApp.Datacontext;
using Newtonsoft.Json;
using System;
using System.Text.Json;

namespace ManageEngineWebApp.Controllers
{
    public class WindowsUserDetailsController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public WindowsUserDetailsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431";
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");

        [AuthFilter]
        public async Task<IActionResult> Index(string domain)
        {
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
            var response = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails");

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
