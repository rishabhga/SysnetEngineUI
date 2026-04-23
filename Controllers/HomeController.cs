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
        public IActionResult GetDashboardContext()
        {
            var isSuperAdmin = IsTopLevelAdmin();
            var companyName = HttpContext.Session.GetString("companyName") ?? "";
            return Json(new { isSuperAdmin, companyName });
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

                return Json(new
                {
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
                var query = BuildScopedQuery();

                var tpTask = client.GetAsync($"{_baseUrl}/api/MissingPatch{query}");
                var winTask = client.GetAsync($"{_baseUrl}/api/MissingPatch/windowpatch{query}");

                await Task.WhenAll(tpTask, winTask);

                var tpContent = await tpTask.Result.Content.ReadAsStringAsync();
                var tpPatches = JsonConvert.DeserializeObject<List<dynamic>>(tpContent) ?? new List<dynamic>();

                var winContent = await winTask.Result.Content.ReadAsStringAsync();
                var winPatches = JsonConvert.DeserializeObject<List<dynamic>>(winContent) ?? new List<dynamic>();

                return Json(new
                {
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
                var query = BuildScopedQuery();

                var swTask = client.GetAsync($"{_baseUrl}/api/Zabbix{query}");
                var statusTask = client.GetAsync($"{_baseUrl}/api/Zabbix/AllDeviceStatuses{query}");

                await Task.WhenAll(swTask, statusTask);

                var swContent = await swTask.Result.Content.ReadAsStringAsync();
                var switches = JsonConvert.DeserializeObject<List<dynamic>>(swContent) ?? new List<dynamic>();

                var statusContent = await statusTask.Result.Content.ReadAsStringAsync();
                var statuses = JsonConvert.DeserializeObject<List<dynamic>>(statusContent) ?? new List<dynamic>();

                int upCount = statuses.Count(s => {
                    string st = s.status?.ToString() ?? s.Status?.ToString() ?? "";
                    return st == "UP" || st == "Ok";
                });

                return Json(new
                {
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


        private async Task<List<dynamic>> FetchNotificationsForCurrentUser()
        {
            var client = GetClient();
            var allItems = new List<dynamic>();
            var seenIds = new HashSet<string>();

            if (IsTopLevelAdmin())
            {
                var response = await client.GetAsync(
                    $"{_baseUrl}/api/RamCpuDiskData/notifications/location");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<List<dynamic>>(content)
                                  ?? new List<dynamic>();
                    allItems.AddRange(items);
                }
            }
            else
            {
                var (companyIds, _, _) = GetUserScope();

                if (!companyIds.Any())
                    return allItems;
                foreach (var cid in companyIds)
                {
                    var url = $"{_baseUrl}/api/RamCpuDiskData/notifications/location?companyId={cid}";
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode) continue;

                    var content = await response.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<List<dynamic>>(content)
                                  ?? new List<dynamic>();

                    foreach (var item in items)
                    {
                        string key = item?.id?.ToString() ?? item?.Id?.ToString() ?? Guid.NewGuid().ToString();
                        if (seenIds.Add(key))
                            allItems.Add(item);
                    }
                }
            }

            return allItems;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content("[]", "application/json");

            try
            {
                var notifications = await FetchNotificationsForCurrentUser();
                return Content(JsonConvert.SerializeObject(notifications), "application/json");
            }
            catch
            {
                return Content("[]", "application/json");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content(JsonConvert.SerializeObject(new { items = new List<object>(), count = 0 }), "application/json");

            try
            {
                var notifications = await FetchNotificationsForCurrentUser();
                var json = JsonConvert.SerializeObject(new { items = notifications, count = notifications.Count });
                return Content(json, "application/json");
            }
            catch
            {
                return Content(JsonConvert.SerializeObject(new { items = new List<object>(), count = 0 }), "application/json");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content("{\"count\":0}", "application/json");

            try
            {
                var notifications = await FetchNotificationsForCurrentUser();

                int unread = notifications.Count(n => {
                    var isReadVal = n?.isRead ?? n?.IsRead;
                    if (isReadVal == null) return true;
                    return !(bool)isReadVal;
                });

                return Content(JsonConvert.SerializeObject(new { count = unread }), "application/json");
            }
            catch
            {
                return Content("{\"count\":0}", "application/json");
            }
        }

        public IActionResult Notifications()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
                return RedirectToAction("Login", "Auth");

            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return RedirectToAction("Index");

            return View();
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