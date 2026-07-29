using ManageEngineWebApp.Models;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
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
    [AuthFilter]
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
        public async Task<IActionResult> GetNetworkStats(int? companyId, int? groupId, int? locationId)
        {
            try
            {
                var client = GetClient();
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var totalTask = client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser{query}");
                var activeTask = client.GetAsync($"{_baseUrl}/api/Command/GetConnectedDevices");
                var remoteTask = client.GetAsync($"{_baseUrl}/api/RemoteAccess/ActiveSessionsCount{query}");

                await Task.WhenAll(totalTask, activeTask, remoteTask);

                var totalContent = await totalTask.Result.Content.ReadAsStringAsync();
                var allDevices = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(totalContent) ?? new List<WindowsUserDetails>();

                var activeContent = await activeTask.Result.Content.ReadAsStringAsync();
                var activeDevices = JsonConvert.DeserializeObject<List<ConnectedClientDto>>(activeContent) ?? new List<ConnectedClientDto>();

                int remoteActiveCount = 0;
                var remoteResp = await remoteTask;
                if (remoteResp.IsSuccessStatusCode)
                {
                    var remoteContent = await remoteResp.Content.ReadAsStringAsync();
                    var remoteObj = JsonConvert.DeserializeObject<dynamic>(remoteContent);
                    remoteActiveCount = (int)(remoteObj?.activeCount ?? 0);
                }

                var activeIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in activeDevices)
                {
                    if (!string.IsNullOrEmpty(d.ClientId)) activeIdentifiers.Add(d.ClientId.Trim());
                    if (!string.IsNullOrEmpty(d.UserName)) activeIdentifiers.Add(d.UserName.Trim());
                }

                var onlineCount = allDevices.Count(d =>
                    (!string.IsNullOrEmpty(d.UserCode) && activeIdentifiers.Contains(d.UserCode.Trim())) ||
                    (!string.IsNullOrEmpty(d.DomainName) && activeIdentifiers.Contains(d.DomainName.Trim()))
                );

                return Json(new
                {
                    total = allDevices.Count,
                    online = onlineCount,
                    offline = Math.Max(0, allDevices.Count - onlineCount),
                    remoteConnected = remoteActiveCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "An internal server error occurred." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPatchOverview(int? companyId, int? groupId, int? locationId)
        {
            try
            {
                var client = GetClient();
                var query = BuildScopedQuery(companyId, locationId, groupId);

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
                return Json(new { error = "An internal server error occurred." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSwitchStatus(int? companyId, int? groupId, int? locationId)
        {
            try
            {
                var client = GetClient();
                var query = BuildScopedQuery(companyId, locationId, groupId);

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
                return Json(new { error = "An internal server error occurred." });
            }
        }


        private async Task<List<dynamic>> FetchNotificationsForCurrentUser(int? companyId = null, int? groupId = null, int? locationId = null)
        {
            var client = GetClient();
            var allItems = new List<dynamic>();
            var seenIds = new HashSet<string>();

            if (IsTopLevelAdmin() && !companyId.HasValue && !groupId.HasValue && !locationId.HasValue)
            {
                var response = await client.GetAsync(
                    $"{_baseUrl}/api/RamCpuDiskData/notifications/by-location");

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
                var userScope = GetUserScope();
                var targetCompanyIds = new List<int>();

                if (companyId.HasValue && companyId.Value > 0)
                {
                    if (IsAuthorized(companyId)) targetCompanyIds.Add(companyId.Value);
                }
                else if (userScope.companyIds.Any())
                {
                    targetCompanyIds.AddRange(userScope.companyIds);
                }
                else if (IsTopLevelAdmin())
                {
                    // For admins with no specific company filter, we might need a different approach or fetch all
                }

                if (!targetCompanyIds.Any() && !IsTopLevelAdmin())
                    return allItems;

                // Build query for the service
                var query = BuildScopedQuery(companyId, locationId, groupId);

                var url = $"{_baseUrl}/api/RamCpuDiskData/notifications/by-location{query}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
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
        public async Task<IActionResult> GetRecentActivity(int? companyId, int? groupId, int? locationId)
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content("[]", "application/json");

            try
            {
                var notifications = await FetchNotificationsForCurrentUser(companyId, groupId, locationId);
                return Content(JsonConvert.SerializeObject(notifications), "application/json");
            }
            catch
            {
                return Content("[]", "application/json");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(int? companyId = null, int? groupId = null, int? locationId = null)
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content(JsonConvert.SerializeObject(new { items = new List<object>(), count = 0 }), "application/json");

            try
            {
                var notifications = await FetchNotificationsForCurrentUser(companyId, groupId, locationId);
                var json = JsonConvert.SerializeObject(new { items = notifications, count = notifications.Count });
                return Content(json, "application/json");
            }
            catch
            {
                return Content(JsonConvert.SerializeObject(new { items = new List<object>(), count = 0 }), "application/json");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveResourceUsage()
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content("[]", "application/json");

            try
            {
                var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/RamCpuDiskData/live-usage");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Content("[]", "application/json");
            }
            catch
            {
                return Content("[]", "application/json");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveResourceHistory()
        {
            if (!IsTopLevelAdmin() && !HasPermission("ComputerSummary.VIP"))
                return Content("[]", "application/json");

            try
            {
                var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/RamCpuDiskData/live-usage-history");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Content("[]", "application/json");
            }
            catch
            {
                return Content("[]", "application/json");
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
                return StatusCode(500, new { error = "An internal server error occurred." });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public class NavigationContext
        {
            public int? CompanyId { get; set; }
            public string CompanyName { get; set; }
            public int? GroupId { get; set; }
            public string GroupName { get; set; }
            public int? LocationId { get; set; }
            public string LocationName { get; set; }
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult EncryptContext([FromBody] NavigationContext context)
        {
            try
            {
                string json = JsonConvert.SerializeObject(context);
                string token = ManageEngineWebApp.Helpers.EncryptionHelper.Encrypt(json);
                return Json(new { success = true, token });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to encrypt context" });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult EncryptDict([FromBody] Dictionary<string, string> data)
        {
            try
            {
                var q = ManageEngineWebApp.Helpers.EncryptionHelper.EncryptParams(data);
                return Json(new { success = true, q });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    }
}