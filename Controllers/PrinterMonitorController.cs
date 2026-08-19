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
    public class PrinterMonitorController : BaseController
    {
        public PrinterMonitorController(IHttpClientFactory httpClientFactory, IConfiguration config)
            : base(httpClientFactory, config)
        {
        }

        [DynamicPermission("PrinterMonitor.View", "View Printer Monitor")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(
            string? q = null,
            int? comId = null, int? groupId = null, int? locationId = null,
            string? companyName = null, string? groupName = null, string? locationName = null)
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

            var printers = new List<PrinterConfiguration>();
            var statuses = new Dictionary<string, PrinterInformation>(StringComparer.OrdinalIgnoreCase);

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
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Locations>();
                }
            }
            catch { }
            if (!isTopAdmin && userLocationIds.Count == 1
                && !comId.HasValue && !groupId.HasValue && !locationId.HasValue)
            {
                locationId = userLocationIds[0];
            }
            ViewBag.SingleLocationUser = !isTopAdmin && userLocationIds.Count == 1;

            if (locationId.HasValue && string.IsNullOrEmpty(locationName))
            {
                var loc = allLocations.FirstOrDefault(l => l.Id == locationId.Value);
                if (loc != null)
                {
                    locationName = loc.LocationName;
                    if (!groupId.HasValue) groupId = loc.GroupsID;
                    if (!comId.HasValue) comId = loc.CompanyID;
                }
            }

            try
            {
                var query = BuildScopedQuery(comId, locationId, groupId);
                var resp = await client.GetAsync($"{_baseUrl}/api/Printer{query}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    printers = System.Text.Json.JsonSerializer.Deserialize<List<PrinterConfiguration>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PrinterConfiguration>();
                }
            }
            catch
            {
                TempData["Error"] = "Could not connect to API. Check that the API service is running.";
            }

            bool isFilterActive = comId.HasValue || groupId.HasValue || locationId.HasValue;
            if (!isTopAdmin && userLocationIds.Any())
            {
                var allowed = userLocationIds.ToHashSet();
                printers = printers.Where(pr => pr.LocationId.HasValue && allowed.Contains(pr.LocationId.Value)).ToList();
            }

            foreach (var pr in printers)
            {
                var loc = allLocations.FirstOrDefault(l => l.Id == pr.LocationId);
                pr.LocationName = loc?.LocationName ?? (pr.LocationId.HasValue ? $"Location #{pr.LocationId}" : "Unassigned");
            }

            try
            {
                var statusResp = await client.GetAsync($"{_baseUrl}/api/Printer/LatestStatuses");
                if (statusResp.IsSuccessStatusCode)
                {
                    var json = await statusResp.Content.ReadAsStringAsync();
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<PrinterInformation>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (list != null)
                        foreach (var s in list)
                        {
                            if (!string.IsNullOrWhiteSpace(s.IPAddress))
                                statuses[s.IPAddress] = s;
                        }
                }
            }
            catch { }

            ViewBag.Statuses = statuses;
            ViewBag.CompanyId = comId;
            ViewBag.GroupId = groupId;
            ViewBag.LocationId = locationId;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;

            return View(printers);
        }

        [DynamicPermission("PrinterMonitor.View", "View Printer Details")]
        public async Task<IActionResult> Details(
            int id,
            int? comId = null, int? groupId = null, int? locationId = null,
            string? companyName = null, string? groupName = null, string? locationName = null)
        {
            using var client = GetClient();

            PrinterConfiguration? printer = null;
            try { printer = await client.GetFromJsonAsync<PrinterConfiguration>($"{_baseUrl}/api/Printer/{id}"); }
            catch { }

            if (printer == null) return NotFound();

            PrinterInformation? latest = null;
            var history = new List<PrinterInformation>();
            var consumables = new List<PrinterConsumable>();

            var printerIp = printer.IPAddress;

            try
            {
                var histResp = await client.GetAsync($"{_baseUrl}/api/Printer/History?printerIp={Uri.EscapeDataString(printerIp)}&take=20");
                if (histResp.IsSuccessStatusCode)
                {
                    var json = await histResp.Content.ReadAsStringAsync();
                    history = System.Text.Json.JsonSerializer.Deserialize<List<PrinterInformation>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PrinterInformation>();
                    latest = history.FirstOrDefault();
                }
            }
            catch { }

            try
            {
                var consResp = await client.GetAsync($"{_baseUrl}/api/Printer/Consumables?printerIp={Uri.EscapeDataString(printerIp)}");
                if (consResp.IsSuccessStatusCode)
                {
                    var json = await consResp.Content.ReadAsStringAsync();
                    consumables = System.Text.Json.JsonSerializer.Deserialize<List<PrinterConsumable>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<PrinterConsumable>();
                }
            }
            catch { }

            ViewBag.Printer = printer;
            ViewBag.History = history;
            ViewBag.Consumables = consumables;

            ViewBag.ComId = comId;
            ViewBag.GroupId = groupId;
            ViewBag.LocationId = locationId;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;

            return View(latest ?? new PrinterInformation
            {
                IPAddress = printer.IPAddress,
                PrinterStatus = "Not scanned yet",
                Consumables = new List<PrinterConsumable>()
            });
        }

        [DynamicPermission("PrinterMonitor.Create", "Create Printer")]
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

            return PartialView("_PrinterForm", new PrinterConfiguration
            {
                IsEnabled = true,
                Port = 161,
                Community = "public",
                SNMPVersion = "V2",
                CompanyId = comId,
                GroupId = groupId,
                LocationId = locationId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("PrinterMonitor.Create", "Create Printer")]
        public async Task<IActionResult> Create([FromForm] PrinterConfiguration model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            using var client = GetClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/Printer/Add", model);
                var respBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var ok = JObject.Parse(respBody);
                        return Json(new
                        {
                            success = true,
                            message = ok["message"]?.ToString(),
                            scanned = ok["scanned"]?.ToObject<bool>() ?? false
                        });
                    }
                    catch
                    {
                        return Json(new { success = true });
                    }
                }

                var apiErrorMsg = $"API error: {response.StatusCode}";
                try
                {
                    var errorObj = JObject.Parse(respBody);
                    if (errorObj["error"] != null) apiErrorMsg += " - " + errorObj["error"]!.ToString();
                    else if (errorObj["message"] != null) apiErrorMsg += " - " + errorObj["message"]!.ToString();
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(respBody)) apiErrorMsg += " - " + respBody;
                }

                return Json(new { success = false, message = apiErrorMsg });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Connection error: An internal server error occurred." });
            }
        }

        [DynamicPermission("PrinterMonitor.Edit", "Edit Printer")]
        public async Task<IActionResult> Edit(
            int id = 0, int? comId = null, int? groupId = null, int? locationId = null, string? q = null)
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
            PrinterConfiguration? printer = null;
            try { printer = await client.GetFromJsonAsync<PrinterConfiguration>($"{_baseUrl}/api/Printer/{id}"); }
            catch { }

            if (printer == null) return NotFound();

            var activeLocId = locationId ?? printer.LocationId;
            await LoadLocationsToViewBagAsync(comId, groupId, activeLocId);

            return PartialView("_PrinterForm", printer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("PrinterMonitor.Edit", "Edit Printer")]
        public async Task<IActionResult> Edit(int id, [FromForm] PrinterConfiguration model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data." });

            model.Id = id;
            using var client = GetClient();
            try
            {
                var response = await client.PutAsJsonAsync($"{_baseUrl}/api/Printer/{id}", model);
                if (response.IsSuccessStatusCode)
                    return Json(new { success = true });

                var body = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {response.StatusCode} - {body}" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Connection error: An internal server error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("PrinterMonitor.Delete", "Delete Printer")]
        public async Task<IActionResult> Delete(int id)
        {
            using var client = GetClient();
            try
            {
                var response = await client.DeleteAsync($"{_baseUrl}/api/Printer/{id}");
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
        [DynamicPermission("PrinterMonitor.Action", "Scan Printer")]
        public async Task<IActionResult> ScanPrinter(int id)
        {
            using var client = GetClient();
            try
            {
                var response = await client.GetAsync($"{_baseUrl}/api/Printer/Scan/{id}");
                var body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var info = JObject.Parse(body);
                        bool online = info["isOnline"]?.ToObject<bool>() ?? false;
                        string errorStatus = info["errorStatus"]?.ToString();
                        return Json(new
                        {
                            success = true,
                            scanned = online,
                            message = online ? "Printer scanned successfully." : (string.IsNullOrWhiteSpace(errorStatus) ? "Scan ran, but the printer did not respond." : errorStatus)
                        });
                    }
                    catch
                    {
                        return Json(new { success = true });
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return Json(new { success = false, message = "Printer not found." });

                try
                {
                    var err = JObject.Parse(body);
                    string msg = err["message"]?.ToString();
                    string detail = err["error"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(msg))
                        return Json(new { success = false, message = string.IsNullOrWhiteSpace(detail) ? msg : $"{msg} ({detail})" });
                }
                catch { }

                return Json(new { success = false, message = $"Scan failed (HTTP {(int)response.StatusCode}) - {body}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Could not reach API: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DynamicPermission("PrinterMonitor.Action", "Trigger Scan")]
        public async Task<IActionResult> TriggerScan(
            string? q = null,
            int? comId = null, int? groupId = null, int? locationId = null,
            string? companyName = null, string? groupName = null, string? locationName = null)
        {
            using var client = GetClient();
            try
            {
                var query = BuildScopedQuery(comId, locationId, groupId);
                var response = await client.GetAsync($"{_baseUrl}/api/Printer/ScanAll{query}");
                if (response.IsSuccessStatusCode)
                    TempData["Message"] = "Scan completed successfully.";
                else
                    TempData["Error"] = $"Scan failed (HTTP {(int)response.StatusCode}). Check API logs.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not reach API: {ex.Message}";
            }

            return RedirectToAction(nameof(Index),
                new { q, comId, groupId, locationId, companyName, groupName, locationName });
        }

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
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Locations>();

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

        private static string BuildScopedQuery(int? companyId, int? locationId, int? groupId)
        {
            var parts = new List<string>();
            if (companyId.HasValue && companyId.Value > 0) parts.Add($"companyId={companyId.Value}");
            if (groupId.HasValue && groupId.Value > 0) parts.Add($"groupId={groupId.Value}");
            if (locationId.HasValue && locationId.Value > 0) parts.Add($"locationId={locationId.Value}");
            return parts.Any() ? "?" + string.Join("&", parts) : string.Empty;
        }
    }
}