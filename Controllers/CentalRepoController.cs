using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

using ManageEngineWebApp.Datacontext;
using Microsoft.Extensions.Configuration;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class CentalRepoController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly string _baseUrl;

        public CentalRepoController(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadSoftware(
            IFormFile file,
            string softwareName,
            string version)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Please select software file.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(softwareName))
                {
                    TempData["Error"] = "Software name is required.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(version))
                {
                    TempData["Error"] = "Version is required.";
                    return RedirectToAction("Index");
                }

                using var client = GetClient();

                using var form = new MultipartFormDataContent();

                // File content
                using var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

                form.Add(fileContent, "file", file.FileName);
                form.Add(new StringContent(softwareName), "softwareName");
                form.Add(new StringContent(version), "version");

                // API call
                var response = await client.PostAsync(
                    $"{_baseUrl}/api/SoftwareRepoDetails/upload",
                    form);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Software uploaded successfully.";
                }
                else
                {
                    TempData["Error"] = $"Upload failed: {responseContent}";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}