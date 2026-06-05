using ManageEngineWebApp.Attributes;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class SoftwareRepoController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public SoftwareRepoController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = config["ApiSettings:BaseUrl"];
        }

                private HttpClient GetClient() 
        {
            var client = _httpClientFactory.CreateClient("ManageEngineApi");
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token)) { client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); }
            return client;
        }

        [HttpGet]
        [DynamicPermission("SoftwareRepo.View", "View Software Repository")]
        public async Task<IActionResult> Index()
        {
            var items = new List<SoftwareRepoDetails>();
            using var client = GetClient();
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/SoftwareRepoDetails");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    items = JsonSerializer.Deserialize<List<SoftwareRepoDetails>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<SoftwareRepoDetails>();
                }
                else
                {
                    TempData["Error"] = $"API returned {(int)response.StatusCode}.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not reach API: {ex.Message}";
            }

            return View(items);
        }

        [HttpGet]
        [DynamicPermission("SoftwareRepo.Upload", "Upload Software")]
        public IActionResult Upload() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("SoftwareRepo.Upload", "Upload Software")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> Upload(IFormFile? file, string? softwareName, string? version)
        {
            var errors = new List<string>();
            if (file == null || file.Length == 0) errors.Add("Please select a file.");
            if (string.IsNullOrWhiteSpace(softwareName)) errors.Add("Software name is required.");
            if (string.IsNullOrWhiteSpace(version)) errors.Add("Version is required.");

            if (errors.Count > 0)
                return Json(new { success = false, errors });

            using var client = GetClient();
            using var content = new MultipartFormDataContent();

            await using var fileStream = file!.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    file.ContentType ?? "application/octet-stream");

            content.Add(fileContent, "file", Path.GetFileName(file.FileName));
            content.Add(new StringContent(softwareName!.Trim()), "softwareName");
            content.Add(new StringContent(version!.Trim()), "version");

            try
            {
                var response = await client.PostAsync(
                    $"{_baseUrl}/api/SoftwareRepoDetails/upload", content);

                if (response.IsSuccessStatusCode)
                    return Json(new
                    {
                        success = true,
                        message = $"{softwareName} v{version} uploaded successfully.",
                        redirect = Url.Action("Index")
                    });

                var body = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, errors = new[] { $"API error ({(int)response.StatusCode}): {body}" } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errors = new[] { $"Connection error: {"An internal server error occurred."}" } });
            }
        }
    }
}