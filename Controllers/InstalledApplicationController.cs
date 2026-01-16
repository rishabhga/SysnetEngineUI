using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;

namespace ManageEngineWebApp.Controllers
{


    [AuthFilter]
    public class InstalledApplicationController : Controller
    {

        private readonly HttpClient _httpClient;
        public InstalledApplicationController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/InstalledApplication");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                return View(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
    }
}
