using ManageEngineWebApp.Models;     
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ManageEngineWebApp.Controllers
{
    public class SoftwareRepoController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public SoftwareRepoController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = config["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431";
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");

        public async Task<IActionResult> Index()
        {
            var softwareList = new List<SoftwareRepoDetails>();
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/SoftwareRepoDetails");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    softwareList = JsonSerializer.Deserialize<List<SoftwareRepoDetails>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<SoftwareRepoDetails>();
                }
                else
                {
                    TempData["Error"] = $"Failed to load software list (HTTP {(int)response.StatusCode}).";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not connect to API: {ex.Message}";
            }

            return View(softwareList);
        }


        public IActionResult Upload()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Upload(string softwareName, string version, IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                ModelState.AddModelError("softwareName", "Software name is required.");
            if (string.IsNullOrWhiteSpace(version))
                ModelState.AddModelError("version", "Version is required.");
            if (file == null || file.Length == 0)
                ModelState.AddModelError("file", "Please select a file.");

            if (!ModelState.IsValid)
                return View();

            try
            {
                using var client = GetClient();
                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(softwareName!), "softwareName");
                formData.Add(new StringContent(version!), "version");
                formData.Add(new StreamContent(file!.OpenReadStream()), "file", file.FileName);

                var response = await client.PostAsync($"{_baseUrl}/api/SoftwareRepoDetails/upload", formData);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = $"✓ \"{softwareName}\" v{version} uploaded successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Upload failed: {error}";
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not connect to API: {ex.Message}";
                return View();
            }
        }
    }
}