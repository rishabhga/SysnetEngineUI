using ManageEngineWebApp.Attributes;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class ClientVersionController : BaseController
    {
        private readonly IWebHostEnvironment _env;

        public ClientVersionController(IHttpClientFactory httpClientFactory, IConfiguration config, IWebHostEnvironment env)
            : base(httpClientFactory, config)
        {
            _env = env;
        }

        [DynamicPermission("ClientVersion.View", "View Client Version")]
        public async Task<IActionResult> Index()
        {
            using var client = GetClient();
            var model = new Models.VersionInfoModel();

            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/ClientVersionControl/GetVersionList");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Models.VersionInfoModel>();
                    if (result != null)
                        model = result;
                }
            }
            catch
            {
                TempData["Error"] = "Failed to connect to API to get current version.";
            }

            bool isAutoUpdateOn = false;
            int currentInterval = 0;
            try
            {
                var statusResp = await client.GetAsync($"{_baseUrl}/api/ClientAutoUpdater/Status");
                if (statusResp.IsSuccessStatusCode)
                {
                    var status = await statusResp.Content.ReadFromJsonAsync<AutoUpdateStatusDto>();
                    if (status != null)
                    {
                        isAutoUpdateOn = status.IsOn;
                        currentInterval = status.IntervalSeconds; 
                    }
                }
            }
            catch { }

            ViewBag.IsAutoUpdateOn = isAutoUpdateOn;
            ViewBag.CurrentInterval = currentInterval; 
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("ClientVersion.AutoUpdate", "Trigger Auto Update")]
        public async Task<IActionResult> TriggerAutoUpdate(string message, int intervalSeconds)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction("Index");
            }

            if (message == "On" && intervalSeconds <= 0)
            {
                TempData["Error"] = "Interval must be greater than 0 seconds.";
                return RedirectToAction("Index");
            }

            try
            {
                using var client = GetClient();
                var payload = new { message = message, intervalSeconds = intervalSeconds };
                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/ClientAutoUpdater/AutoUpdate", payload);

                if (response.IsSuccessStatusCode)
                {
                    if (message == "On")
                    {
                        int mins = intervalSeconds / 60;
                        TempData["Message"] = $"Auto-update turned ON. Clients will check every {mins} minute(s).";
                    }
                    else
                    {
                        TempData["Message"] = "Auto-update turned OFF. All clients notified.";
                    }
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(body);
                        var msg = doc.RootElement.TryGetProperty("msg", out var msgProp)
                            ? msgProp.GetString() : "Unknown API error";

                        TempData["Error"] = (response.StatusCode == System.Net.HttpStatusCode.NotFound
                            && msg != null && msg.Contains("No clients connected"))
                            ? "No clients are currently connected to receive the update."
                            : $"API Error: {msg}";
                    }
                    catch
                    {
                        TempData["Error"] = $"API returned {(int)response.StatusCode}: {body}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to reach API: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        [RequestSizeLimit(long.MaxValue)]
        [DynamicPermission("ClientVersion.Upload", "Upload Client Version")]
        public async Task<IActionResult> Upload(IFormFile versionFile, string newVersion)
        {
            if (versionFile == null || versionFile.Length == 0)
            {
                TempData["Error"] = "Please select a valid file.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(newVersion))
            {
                TempData["Error"] = "Please enter a version number.";
                return RedirectToAction("Index");
            }

            // Validate version format on MVC side too
            if (!System.Text.RegularExpressions.Regex.IsMatch(newVersion, @"^\d+\.\d+\.\d+(\.\d+)?$"))
            {
                TempData["Error"] = "Invalid version format. Use format like 1.0.1 or 1.0.0.5";
                return RedirectToAction("Index");
            }

            if (!System.Version.TryParse(newVersion, out var parsedVersion) || parsedVersion < new System.Version(1, 0, 1))
            {
                TempData["Error"] = "Version must be at least 1.0.1";
                return RedirectToAction("Index");
            }

            try
            {
                using var client = GetClient();
                using var content = new MultipartFormDataContent();      
                content.Add(new StringContent(newVersion), "version");
                using var stream = versionFile.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(versionFile.ContentType);
                content.Add(fileContent, "versionFile", versionFile.FileName);
                var response = await client.PostAsync($"{_baseUrl}/api/ClientVersionControl/UpdateVersion", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Client version uploaded and updated successfully.";
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(body);
                        var msg = doc.RootElement.TryGetProperty("message", out var msgProp)
                            ? msgProp.GetString() : null;
                        TempData["Error"] = !string.IsNullOrEmpty(msg) ? msg : $"Upload failed. Status: {response.StatusCode}";
                    }
                    catch
                    {
                        TempData["Error"] = $"Upload failed. Status: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}