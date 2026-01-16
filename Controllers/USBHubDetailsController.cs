using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;

namespace ManageEngineWebApp.Controllers
{
    public class USBHubDetailsController : Controller
    {

        private readonly HttpClient _httpClient;
        public USBHubDetailsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/USBHubDetails");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBHubDetails>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                return View(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
    }
}
