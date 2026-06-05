using Microsoft.AspNetCore.Mvc;
using ManageEngineWebApp.Datacontext;
using Newtonsoft.Json;
using System.Text;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class DeviceSecurityController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public DeviceSecurityController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
            : base(httpClientFactory, configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "";
        }

        public async Task<IActionResult> Index()
        {
            // Validate session and scope if necessary
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSecurityStatus()
        {
            try
            {
                var client = GetClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/USbBlockingAndUnBlocking");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = System.Text.Json.JsonSerializer.Deserialize<object>(content);
                    return Json(new { success = true, data = data });
                }

                return Json(new { success = false, message = "Failed to load data from API." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An internal server error occurred." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUsbStatus([FromBody] ToggleUsbRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ClientId))
                    return Json(new { success = false, message = "Client ID is required." });

                var client = GetClient();
                string action = request.Block ? "block-usb" : "unblock-usb";
                var response = await client.PostAsync($"{_apiBaseUrl}/api/USbBlockingAndUnBlocking/{action}/{request.ClientId}", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Json(new { success = true, message = $"USB {(request.Block ? "Blocked" : "Unblocked")} successfully for {request.ClientId}." });
                }

                return Json(new { success = false, message = "Failed to communicate with device." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An internal server error occurred." });
            }
        }
    }

    public class ToggleUsbRequest
    {
        public string ClientId { get; set; } = "";
        public bool Block { get; set; }
    }
}
