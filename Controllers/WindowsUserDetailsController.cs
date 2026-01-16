using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Text.Json;

namespace ManageEngineWebApp.Controllers
{
    public class WindowsUserDetailsController : Controller
    {

        private readonly HttpClient _httpClient;
        public WindowsUserDetailsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<IActionResult> Index(string domain)
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowsUserDetails");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
               var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                var datalist = data.Where(x => x.DomainName == domain).ToList();
                return View(datalist);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
        public async Task<IActionResult> Userview()
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowsUserDetails");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                var datalist  = data.Where(x=>x.Status == "Enabled").ToList();
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
