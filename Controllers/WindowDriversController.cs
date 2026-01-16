using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;

namespace ManageEngineWebApp.Controllers
{
    public class WindowDriversController : Controller
    {

        private readonly HttpClient _httpClient;
        public WindowDriversController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowDrivers");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);

                ViewBag.WindowDrivers = data;

                return View(data);
                //return Json(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }



        public async Task<IActionResult> datalist()
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowDrivers");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                //return View(data);
                return Json(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
        public async Task<IActionResult> users()
        {
           
            var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowsUserDetails");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                //return View(data);
                return Json(data);
            }

            throw new Exception("Unable to fetch data from the API.");
        }
        public async Task<IActionResult> Services()
        {
            var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowDrivers");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                //var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                //return View(data);



                return Json(data);
            }





            throw new Exception("Unable to fetch data from the API.");
        }

    }
}
