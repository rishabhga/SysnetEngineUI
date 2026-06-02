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
            int? comId, int? groupId, int? locationId, 
            string companyName, string groupName, string locationName, 
            int? companyid, int? groupid, int? locationid)
        {
            var activeComId = comId ?? companyid;
            var activeGroupId = groupId ?? groupid;
            var activeLocationId = locationId ?? locationid;

            var switches = new List<SwitchMaster>();
            var deviceStatuses = new Dictionary<int, DeviceStatus>();

            using var client = GetClient();

            // Fetch all locations to do dynamic mapping and names resolution
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

            // Resolve names if IDs are present but names are missing
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

            // Fetch switches
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

            // Filtering based on hierarchy parameters and user mapping permissions
            var userLocationIds = RoleHelper.GetLocationIds(HttpContext);
            bool isTopAdmin = RoleHelper.IsTopLevelAdmin(HttpContext);
            bool isFilterActive = activeComId.HasValue || activeGroupId.HasValue || activeLocationId.HasValue;

            HashSet<int> allowedLocIds = null;

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
                {
                    filteredLocs = filteredLocs.Where(l => userLocationIds.Contains(l.Id)).ToList();
                }
                allowedLocIds = filteredLocs.Select(l => l.Id).ToHashSet();
            }
            else if (!isTopAdmin && userLocationIds.Any())
            {
                allowedLocIds = userLocationIds.ToHashSet();
            }

            if (allowedLocIds != null)
            {
                switches = switches.Where(s => s.LocationId.HasValue && allowedLocIds.Contains(s.LocationId.Value)).ToList();
            }

            // Fetch statuses
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

            // Fetch and map WindowsUserDetails for mapped user details inside switch cards
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
        public async Task<IActionResult> Create(int? comId, int? groupId, int? locationId)
        {
            await LoadLocationsToViewBagAsync(comId, groupId, locationId);
            await LoadDevicesToViewBagAsync(comId, groupId, locationId);

            return PartialView("_SwitchForm", new SwitchMaster
            {
                IsActive = true,
                DeviceType = "Switch",
                Community = "public",
                LocationId = locationId
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
                        apiErrorMsg += " - " + errorObj["error"].ToString();
                    else if (errorObj["message"] != null)
                        apiErrorMsg += " - " + errorObj["message"].ToString();
                }
                catch { }

                return Json(new { success = false, message = apiErrorMsg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection error: {"An internal server error occurred."}" });
            }
        }

        [DynamicPermission("SwitchMonitor.Edit", "Edit Switch")]
        public async Task<IActionResult> Edit(int id, int? comId, int? groupId, int? locationId)
        {
            using var client = GetClient();
            SwitchMaster? sw = null;
            try { sw = await client.GetFromJsonAsync<SwitchMaster>($"{_baseUrl}/api/Zabbix/{id}"); }
            catch { }

            if (sw == null) return NotFound();

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
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection error: {"An internal server error occurred."}" });
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection error: {"An internal server error occurred."}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("SwitchMonitor.Action", "Trigger Poll")]
        public async Task<IActionResult> TriggerPoll()
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

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLocationsToViewBagAsync(int? comId = null, int? groupId = null, int? locationId = null)
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
                    var allLocations = System.Text.Json.JsonSerializer.Deserialize<List<Locations>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Locations>();
                    
                    var filtered = allLocations;

                    if (locationId.HasValue && locationId.Value > 0)
                        filtered = filtered.Where(l => l.Id == locationId.Value).ToList();
                    else if (groupId.HasValue && groupId.Value > 0)
                        filtered = filtered.Where(l => l.GroupsID == groupId.Value).ToList();
                    else if (comId.HasValue && comId.Value > 0)
                        filtered = filtered.Where(l => l.CompanyID == comId.Value).ToList();

                    if (RoleHelper.IsTopLevelAdmin(HttpContext) || !userLocationIds.Any())
                    {
                        locations = filtered;
                    }
                    else
                    {
                        locations = filtered.Where(l => userLocationIds.Contains(l.Id)).ToList();
                    }
                }
            }
            catch { }
            ViewBag.Locations = locations;
        }

        private async Task LoadDevicesToViewBagAsync(int? companyId, int? groupId, int? locationId)
        {
            using var client = GetClient();
            var query = BuildScopedQuery(companyId, locationId, groupId);
            var url = $"api/WindowsUserDetails/allUser{query}";
            var devices = new List<WindowsUserDetails>();
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/{url}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    devices = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(json) ?? new List<WindowsUserDetails>();
                }
            }
            catch { }

            // Fetch IP addresses from UserDetails api
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
                else
                {
                    userIps["ERROR_API"] = $"Status: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                userIps["ERROR_EX"] = ex.Message;
            }
            ViewBag.UserIps = userIps;

            // Strict Filter: select user only from inside that location (if locationId is active/specified)
            if (locationId.HasValue && locationId.Value > 0)
            {
                devices = devices.Where(d => d.LocationId == locationId.Value).ToList();
            }

            ViewBag.Devices = devices.OrderBy(d => d.DomainName).ToList();
        }
    }
}