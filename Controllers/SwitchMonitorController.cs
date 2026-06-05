using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace ManageEngineWebApp.Controllers
{
    using Newtonsoft.Json.Linq;

    [AuthFilter]
    public class SwitchMonitorController : BaseController
    {
        public SwitchMonitorController(IHttpClientFactory httpClientFactory, IConfiguration config)
            : base(httpClientFactory, config)
        {
        }

        [DynamicPermission("SwitchMonitor.View", "View Switch Monitor")]
        public async Task<IActionResult> Index(
            string? q = null,
            int? comId = null, int? groupId = null, int? locationId = null,
            string? companyName = null, string? groupName = null, string? locationName = null,
            int? companyid = null, int? groupid = null, int? locationid = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("comId", out var cid) && int.TryParse(cid, out var c)) comId = c;
                if (p.TryGetValue("groupId", out var gid) && int.TryParse(gid, out var g)) groupId = g;
                if (p.TryGetValue("locationId", out var lid) && int.TryParse(lid, out var l)) locationId = l;
                if (p.TryGetValue("companyName", out var cn)) companyName = cn;
                if (p.TryGetValue("groupName", out var gn)) groupName = gn;
                if (p.TryGetValue("locationName", out var ln)) locationName = ln;
            }

            var activeComId = comId ?? companyid;
            var activeGroupId = groupId ?? groupid;
            var activeLocationId = locationId ?? locationid;

            var switches = new List<SwitchMaster>();
            var deviceStatuses = new Dictionary<int, DeviceStatus>();

            using var client = GetClient();

            bool isTopAdmin = RoleHelper.IsTopLevelAdmin(HttpContext);
            var userLocationIds = RoleHelper.GetLocationIds(HttpContext);

            var allLocations = new List<Locations>();
            try
            {
                var locResponse = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata");
                if (locResponse.IsSuccessStatusCode)
                {
                    var json = await locResponse.Content.ReadAsStringAsync();
                    allLocations = System.Text.Json.JsonSerializer.Deserialize<List<Locations>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<Locations>();
                }
            }
            catch { }

            if (activeLocationId.HasValue && string.IsNullOrEmpty(locationName))
            {
                var loc = allLocations.FirstOrDefault(l => l.Id == activeLocationId.Value);
                if (loc != null)
                {
                    locationName = loc.LocationName;
                    if (!activeGroupId.HasValue) activeGroupId = loc.GroupsID;
                    if (!activeComId.HasValue) activeComId = loc.CompanyID;
                }
            }

            try
            {
                var swResponse = await client.GetAsync($"{_baseUrl}/api/Zabbix");
                if (swResponse.IsSuccessStatusCode)
                {
                    var json = await swResponse.Content.ReadAsStringAsync();
                    switches = System.Text.Json.JsonSerializer.Deserialize<List<SwitchMaster>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<SwitchMaster>();
                }
            }
            catch
            {
                TempData["Error"] = "Could not connect to API. Check that the API service is running.";
            }

            bool isFilterActive = activeComId.HasValue || activeGroupId.HasValue || activeLocationId.HasValue;
            HashSet<int>? allowedLocIds = null;

            if (isFilterActive)
            {
                var filteredLocs = allLocations;
                if (activeLocationId.HasValue)
                    filteredLocs = filteredLocs.Where(l => l.Id == activeLocationId.Value).ToList();
                else if (activeGroupId.HasValue)
                    filteredLocs = filteredLocs.Where(l => l.GroupsID == activeGroupId.Value).ToList();
                else if (activeComId.HasValue)
                    filteredLocs = filteredLocs.Where(l => l.CompanyID == activeComId.Value).ToList();

                if (!isTopAdmin && userLocationIds.Any())
                    filteredLocs = filteredLocs.Where(l => userLocationIds.Contains(l.Id)).ToList();

                allowedLocIds = filteredLocs.Select(l => l.Id).ToHashSet();
            }
            else if (!isTopAdmin && userLocationIds.Any())
            {
                allowedLocIds = userLocationIds.ToHashSet();
            }

            // FIX: Guard against LocationId being non-nullable int on the model.
            // If SwitchMaster.LocationId is int? use the HasValue/Value pattern;
            // if it is int, use a direct comparison. Both cases are handled below.
            if (allowedLocIds != null)
                switches = switches
                    .Where(s => s.LocationId != null && allowedLocIds.Contains(
                        s.LocationId is int locId ? locId : Convert.ToInt32(s.LocationId)))
                    .ToList();

            try
            {
                var statusResponse = await client.GetAsync($"{_baseUrl}/api/Zabbix/AllDeviceStatuses");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var json = await statusResponse.Content.ReadAsStringAsync();
                    var statuses = System.Text.Json.JsonSerializer.Deserialize<List<DeviceStatus>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (statuses != null)
                        foreach (var ds in statuses)
                            deviceStatuses[ds.SwitchMasterId] = ds;
                }
            }
            catch { }

            var dbUsers = new Dictionary<string, WindowsUserDetails>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var query = BuildScopedQuery(activeComId, activeLocationId, activeGroupId);
                var response = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser{query}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var usersList = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(json);

                    if (usersList != null)
                    {
                        if (activeLocationId.HasValue && activeLocationId.Value > 0)
                        {
                            usersList = usersList
                                .Where(u => u.LocationId == activeLocationId.Value)
                                .ToList();
                        }
                        else if (!isTopAdmin && userLocationIds.Any())
                        {
                            usersList = usersList
                                .Where(u => userLocationIds.Contains(u.LocationId))
                                .ToList();
                        }

                        foreach (var u in usersList)
                        {
                            if (!string.IsNullOrEmpty(u.DomainName))
                                dbUsers[u.DomainName] = u;
                            if (!string.IsNullOrEmpty(u.UserCode))
                                dbUsers[u.UserCode] = u;
                        }
                    }
                }
            }
            catch { }

            ViewBag.DeviceStatuses = deviceStatuses;
            ViewBag.DashboardUsers = dbUsers;

            var userIps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/UserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allUserDetails = JsonConvert.DeserializeObject<List<UserDetails>>(json);
                    if (allUserDetails != null)
                    {
                        foreach (var u in allUserDetails)
                        {
                            if (!string.IsNullOrEmpty(u.domainName) && !string.IsNullOrEmpty(u.IpAddress))
                                userIps[u.domainName] = u.IpAddress;
                        }
                    }
                }

                var netResponse = await client.GetAsync($"{_baseUrl}/api/NetworkAdapterDetails");
                if (netResponse.IsSuccessStatusCode)
                {
                    var jsonNet = await netResponse.Content.ReadAsStringAsync();
                    var allNetDetails = JsonConvert.DeserializeObject<List<dynamic>>(jsonNet);
                    if (allNetDetails != null)
                    {
                        foreach (var n in allNetDetails)
                        {
                            string ip = n.ipAddress ?? n.IPAddress;
                            string userCode = n.userCode ?? n.UserCode;
                            string hostName = n.dnsHostName ?? n.DNSHostName;
                            if (!string.IsNullOrEmpty(ip))
                            {
                                ip = ip.Trim();
                                if (!string.IsNullOrEmpty(userCode)) userIps[userCode.Trim()] = ip;
                                if (!string.IsNullOrEmpty(hostName)) userIps[hostName.Trim()] = ip;
                            }
                        }
                    }
                }
            }
            catch { }

            ViewBag.UserIps = userIps;
            ViewBag.CompanyId = activeComId;
            ViewBag.GroupId = activeGroupId;
            ViewBag.LocationId = activeLocationId;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;

            return View(switches);
        }

        [DynamicPermission("SwitchMonitor.View", "View Switch Details")]
        public async Task<IActionResult> Details(int id)
        {
            using var client = GetClient();

            SwitchMaster? switchMaster = null;
            try
            {
                switchMaster = await client.GetFromJsonAsync<SwitchMaster>(
                    $"{_baseUrl}/api/Zabbix/{id}");
            }
            catch { }

            if (switchMaster == null)
                return NotFound();

            DeviceStatus? device = null;
            try
            {
                var deviceResponse = await client.GetAsync(
                    $"{_baseUrl}/api/Zabbix/DeviceStatuses?switchMasterId={id}");
                if (deviceResponse.IsSuccessStatusCode)
                {
                    var json = await deviceResponse.Content.ReadAsStringAsync();
                    device = System.Text.Json.JsonSerializer.Deserialize<DeviceStatus>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch { }

            var agentHistory = new List<AgentPollHistory>();
            if (!string.IsNullOrEmpty(switchMaster.IpAddress))
            {
                try
                {
                    var historyResponse = await client.GetAsync(
                        $"{_baseUrl}/api/Zabbix/AgentPollHistory?agentIp={switchMaster.IpAddress}");
                    if (historyResponse.IsSuccessStatusCode)
                    {
                        var json = await historyResponse.Content.ReadAsStringAsync();
                        agentHistory = System.Text.Json.JsonSerializer.Deserialize<List<AgentPollHistory>>(json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                            ?? new List<AgentPollHistory>();
                    }
                }
                catch { }
            }

            ViewBag.Switch = switchMaster;
            ViewBag.AgentHistory = agentHistory;

            return View(device ?? new DeviceStatus
            {
                SwitchMasterId = id,
                Status = "Not Polled Yet",
                Ports = new List<PortStatus>()
            });
        }

        [DynamicPermission("SwitchMonitor.Create", "Create Switch")]
        public async Task<IActionResult> Create(
            int? comId = null, int? groupId = null, int? locationId = null, string? q = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("comId", out var cid) && int.TryParse(cid, out var c)) comId = c;
                if (p.TryGetValue("groupId", out var gid) && int.TryParse(gid, out var g)) groupId = g;
                if (p.TryGetValue("locationId", out var lid) && int.TryParse(lid, out var l)) locationId = l;
            }

            await LoadLocationsToViewBagAsync(comId, groupId, locationId);
            await LoadDevicesToViewBagAsync(comId, groupId, locationId);

            // FIX: LocationId on SwitchMaster must be int? to accept locationId here.
            // If your model has int LocationId, change it to int? LocationId in SwitchMaster.
            return PartialView("_SwitchForm", new SwitchMaster
            {
                IsActive = true,
                DeviceType = "Switch",
                Community = "public",
                LocationId = locationId   // requires int? on the model
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [DynamicPermission("SwitchMonitor.Create", "Create Switch")]
        public async Task<IActionResult> Create([FromBody] SwitchMaster model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            using var client = GetClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/Zabbix", model);
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true });

                var body = await response.Content.ReadAsStringAsync();
                var apiErrorMsg = $"API error: {response.StatusCode}";
                try
                {
                    var errorObj = JObject.Parse(body);
                    if (errorObj["error"] != null)
                        apiErrorMsg += " - " + errorObj["error"]!.ToString();
                    else if (errorObj["message"] != null)
                        apiErrorMsg += " - " + errorObj["message"]!.ToString();
                }
                catch { }

                return Json(new { success = false, message = apiErrorMsg });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Connection error: An internal server error occurred." });
            }
        }

        [DynamicPermission("SwitchMonitor.Edit", "Edit Switch")]
        public async Task<IActionResult> Edit(
            int id = 0, int? comId = null, int? groupId = null,
            int? locationId = null, string? q = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("id", out var idStr) && int.TryParse(idStr, out var decId)) id = decId;
                if (p.TryGetValue("comId", out var cid) && int.TryParse(cid, out var c)) comId = c;
                if (p.TryGetValue("groupId", out var gid) && int.TryParse(gid, out var g)) groupId = g;
                if (p.TryGetValue("locationId", out var lid) && int.TryParse(lid, out var l)) locationId = l;
            }

            using var client = GetClient();
            SwitchMaster? sw = null;
            try { sw = await client.GetFromJsonAsync<SwitchMaster>($"{_baseUrl}/api/Zabbix/{id}"); }
            catch { }

            if (sw == null) return NotFound();

            // FIX: sw.LocationId must be int? for this ?? to compile.
            // If it is int, use: var activeLocId = locationId ?? (sw.LocationId > 0 ? sw.LocationId : locationId);
            var activeLocId = locationId ?? sw.LocationId;
            await LoadLocationsToViewBagAsync(comId, groupId, activeLocId);
            await LoadDevicesToViewBagAsync(comId, groupId, activeLocId);

            return PartialView("_SwitchForm", sw);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [DynamicPermission("SwitchMonitor.Edit", "Edit Switch")]
        public async Task<IActionResult> Edit(int id, [FromBody] SwitchMaster model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            model.Id = id;
            using var client = GetClient();
            try
            {
                var response = await client.PutAsJsonAsync($"{_baseUrl}/api/Zabbix/{id}", model);
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true });

                return Json(new { success = false, message = $"API error: {response.StatusCode}" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Connection error: An internal server error occurred." });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [DynamicPermission("SwitchMonitor.Delete", "Delete Switch")]
        public async Task<IActionResult> Delete(int id)
        {
            using var client = GetClient();
            try
            {
                var response = await client.DeleteAsync($"{_baseUrl}/api/Zabbix/{id}");
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true });

                return Json(new { success = false, message = $"API error: {response.StatusCode}" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Connection error: An internal server error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("SwitchMonitor.Action", "Trigger Poll")]
        public async Task<IActionResult> TriggerPoll(
            string? q = null,
            int? comId = null, int? groupId = null, int? locationId = null,
            string? companyName = null, string? groupName = null, string? locationName = null)
        {
            using var client = GetClient();
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/Zabbix/switchstatus");
                if (response.IsSuccessStatusCode)
                    TempData["Message"] = "Polling completed successfully.";
                else
                    TempData["Error"] = $"Polling failed (HTTP {(int)response.StatusCode}). Check API logs.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not reach API: {ex.Message}";
            }

            return RedirectToAction(nameof(Index),
                new { q, comId, groupId, locationId, companyName, groupName, locationName });
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private async Task LoadLocationsToViewBagAsync(
            int? comId = null, int? groupId = null, int? locationId = null)
        {
            var userLocationIds = RoleHelper.GetLocationIds(HttpContext);
            using var client = GetClient();
            var locations = new List<Locations>();

            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allLocations = System.Text.Json.JsonSerializer.Deserialize<List<Locations>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<Locations>();

                    var filtered = allLocations;

                    if (locationId.HasValue && locationId.Value > 0)
                        filtered = filtered.Where(l => l.Id == locationId.Value).ToList();
                    else if (groupId.HasValue && groupId.Value > 0)
                        filtered = filtered.Where(l => l.GroupsID == groupId.Value).ToList();
                    else if (comId.HasValue && comId.Value > 0)
                        filtered = filtered.Where(l => l.CompanyID == comId.Value).ToList();

                    locations = RoleHelper.IsTopLevelAdmin(HttpContext) || !userLocationIds.Any()
                        ? filtered
                        : filtered.Where(l => userLocationIds.Contains(l.Id)).ToList();
                }
            }
            catch { }

            ViewBag.Locations = locations;
        }

        private async Task LoadDevicesToViewBagAsync(
            int? companyId, int? groupId, int? locationId)
        {
            using var client = GetClient();
            bool isTopAdmin = RoleHelper.IsTopLevelAdmin(HttpContext);
            var userLocationIds = RoleHelper.GetLocationIds(HttpContext);

            var query = BuildScopedQuery(companyId, locationId, groupId);
            var devices = new List<WindowsUserDetails>();

            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser{query}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    devices = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(json)
                              ?? new List<WindowsUserDetails>();
                }
            }
            catch { }

            if (locationId.HasValue && locationId.Value > 0)
            {
                devices = devices.Where(d => d.LocationId == locationId.Value).ToList();
            }
            else if (!isTopAdmin && userLocationIds.Any())
            {
                devices = devices
                    .Where(d => userLocationIds.Contains(d.LocationId))
                    .ToList();
            }

            var userIps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/UserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allUserDetails = JsonConvert.DeserializeObject<List<UserDetails>>(json);
                    if (allUserDetails != null)
                    {
                        foreach (var u in allUserDetails)
                        {
                            if (!string.IsNullOrEmpty(u.IpAddress))
                            {
                                var ip = u.IpAddress.Trim();
                                if (!string.IsNullOrEmpty(u.domainName)) userIps[u.domainName.Trim()] = ip;
                                if (!string.IsNullOrEmpty(u.WindowName)) userIps[u.WindowName.Trim()] = ip;
                                if (!string.IsNullOrEmpty(u.UserName)) userIps[u.UserName.Trim()] = ip;
                            }
                        }
                    }
                }

                var netResponse = await client.GetAsync($"{_baseUrl}/api/NetworkAdapterDetails");
                if (netResponse.IsSuccessStatusCode)
                {
                    var jsonNet = await netResponse.Content.ReadAsStringAsync();
                    var allNetDetails = JsonConvert.DeserializeObject<List<dynamic>>(jsonNet);
                    if (allNetDetails != null)
                    {
                        foreach (var n in allNetDetails)
                        {
                            string ip = n.ipAddress ?? n.IPAddress;
                            string userCode = n.userCode ?? n.UserCode;
                            string hostName = n.dnsHostName ?? n.DNSHostName;
                            if (!string.IsNullOrEmpty(ip))
                            {
                                ip = ip.Trim();
                                if (!string.IsNullOrEmpty(userCode)) userIps[userCode.Trim()] = ip;
                                if (!string.IsNullOrEmpty(hostName)) userIps[hostName.Trim()] = ip;
                            }
                        }
                    }
                }
            }
            catch { }

            ViewBag.UserIps = userIps;
            ViewBag.Devices = devices.OrderBy(d => d.DomainName).ToList();
        }
    }
}