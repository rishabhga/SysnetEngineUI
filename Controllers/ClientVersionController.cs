using ManageEngineWebApp.Attributes;
using ManageEngineWebApp.Datacontext;
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
                    {
                        model = result;
                    }
                }
            }
            catch
            {
                TempData["Error"] = "Failed to connect to API to get current version.";
            }

            return View(model);
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

            try
            {
                // Ensure the directory exists
                string uploadsFolder = Path.Combine(_env.WebRootPath, "ClientVersionUpload");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete any existing files in the folder to keep only the latest version
                var existingFiles = Directory.GetFiles(uploadsFolder);
                foreach (var file in existingFiles)
                {
                    try { System.IO.File.Delete(file); } catch { }
                }

                string fileName = Path.GetFileName(versionFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await versionFile.CopyToAsync(fileStream);
                }

                string downloadUrl = $"/ClientVersionUpload/{fileName}";

                // Update the database via API
                using var client = GetClient();
                var payload = new { version = newVersion, url = downloadUrl };
                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/ClientVersionControl/UpdateVersion", payload);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Client version uploaded and updated successfully.";
                }
                else
                {
                    TempData["Error"] = $"File uploaded, but failed to update API. Status: {response.StatusCode}";
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
