using ManageEngineWebApp.Models;
using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration config) : base(httpClientFactory, config)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNetworkStats()
        {
            try
            {
                var client = GetClient();
                
                var query = BuildScopedQuery();
                var totalTask = client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser{query}");
                var activeTask = client.GetAsync($"{_baseUrl}/api/Command/GetConnectedDevices");

                await Task.WhenAll(totalTask, activeTask);

                var totalContent = await totalTask.Result.Content.ReadAsStringAsync();
                var allDevices = JsonConvert.DeserializeObject<List<dynamic>>(totalContent) ?? new List<dynamic>();
                
                var activeContent = await activeTask.Result.Content.ReadAsStringAsync();
                var activeDevices = JsonConvert.DeserializeObject<List<dynamic>>(activeContent) ?? new List<dynamic>();

                return Json(new {
                    total = allDevices.Count,
                    online = activeDevices.Count,
                    offline = Math.Max(0, allDevices.Count - activeDevices.Count)
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPatchOverview()
        {
            try
            {
                var client = GetClient();
                
                var tpTask = client.GetAsync($"{_baseUrl}/api/MissingPatch");
                var winTask = client.GetAsync($"{_baseUrl}/api/MissingPatch/windowpatch");

                await Task.WhenAll(tpTask, winTask);

                var tpContent = await tpTask.Result.Content.ReadAsStringAsync();
                var tpPatches = JsonConvert.DeserializeObject<List<dynamic>>(tpContent) ?? new List<dynamic>();

                var winContent = await winTask.Result.Content.ReadAsStringAsync();
                var winPatches = JsonConvert.DeserializeObject<List<dynamic>>(winContent) ?? new List<dynamic>();

                return Json(new {
                    thirdPartyCount = tpPatches.Count,
                    windowsCount = winPatches.Count,
                    total = tpPatches.Count + winPatches.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSwitchStatus()
        {
            try
            {
                var client = GetClient();
                
                var swTask = client.GetAsync($"{_baseUrl}/api/Zabbix");
                var statusTask = client.GetAsync($"{_baseUrl}/api/Zabbix/AllDeviceStatuses");

                await Task.WhenAll(swTask, statusTask);

                var swContent = await swTask.Result.Content.ReadAsStringAsync();
                var switches = JsonConvert.DeserializeObject<List<dynamic>>(swContent) ?? new List<dynamic>();

                var statusContent = await statusTask.Result.Content.ReadAsStringAsync();
                var statuses = JsonConvert.DeserializeObject<List<dynamic>>(statusContent) ?? new List<dynamic>();

                int upCount = statuses.Count(s => {
                    string st = s.status?.ToString() ?? s.Status?.ToString() ?? "";
                    return st == "UP" || st == "Ok";
                });

                return Json(new {
                    total = switches.Count,
                    up = upCount,
                    down = Math.Max(0, switches.Count - upCount)
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
            {
                var companyId = RoleHelper.GetCompanyId(HttpContext) ?? 0;
                var groupId = RoleHelper.GetGroupId(HttpContext);
                var locationId = RoleHelper.GetLocationId(HttpContext);

                var client = GetClient();
                string url = $"api/RamCpuDiskData/notifications/location?companyId={companyId}";
                if (groupId.HasValue) url += $"&groupId={groupId}";
                if (locationId.HasValue) url += $"&locationId={locationId}";

                var response = await client.GetAsync($"{_baseUrl}/{url}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return Json(new List<object>());
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardHierarchy()
        {
            try
            {
                var hierarchy = await LoadHierarchyAsync();
                return Json(hierarchy);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
