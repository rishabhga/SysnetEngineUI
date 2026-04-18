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

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class SwitchMonitorController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public SwitchMonitorController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = config["ApiSettings:BaseUrl"];
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");

        [DynamicPermission("SwitchMonitor.View", "View Switch Monitor")]
        public async Task<IActionResult> Index()
        {
            var switches = new List<SwitchMaster>();
            var deviceStatuses = new Dictionary<int, DeviceStatus>();

            using var client = GetClient();

            try
            {
                var swResponse = await client.GetAsync($"{_baseUrl}/api/Zabbix");
                if (swResponse.IsSuccessStatusCode)
                {
                    var json = await swResponse.Content.ReadAsStringAsync();
                    switches = JsonSerializer.Deserialize<List<SwitchMaster>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<SwitchMaster>();
                }
            }
            catch
            {
                TempData["Error"] = "Could not connect to API. Check that the API service is running.";
            }

            try
            {
                var statusResponse = await client.GetAsync($"{_baseUrl}/api/Zabbix/AllDeviceStatuses");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var json = await statusResponse.Content.ReadAsStringAsync();
                    var statuses = JsonSerializer.Deserialize<List<DeviceStatus>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (statuses != null)
                        foreach (var ds in statuses)
                            deviceStatuses[ds.SwitchMasterId] = ds;
                }
            }
            catch {  }

            ViewBag.DeviceStatuses = deviceStatuses;
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
                    device = JsonSerializer.Deserialize<DeviceStatus>(json,
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
                        agentHistory = JsonSerializer.Deserialize<List<AgentPollHistory>>(json,
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
        public IActionResult Create()
        {
            return PartialView("_SwitchForm", new SwitchMaster
            {
                IsActive = true,
                DeviceType = "Switch",
                Community = "public"
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
                return Json(new { success = false, message = $"API error: {response.StatusCode}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Connection error: {ex.Message}" });
            }
        }
        [DynamicPermission("SwitchMonitor.Edit", "Edit Switch")]
        public async Task<IActionResult> Edit(int id)
        {
            using var client = GetClient();
            SwitchMaster? sw = null;
            try { sw = await client.GetFromJsonAsync<SwitchMaster>($"{_baseUrl}/api/Zabbix/{id}"); }
            catch { }

            if (sw == null) return NotFound();
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
                return Json(new { success = false, message = $"Connection error: {ex.Message}" });
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
                return Json(new { success = false, message = $"Connection error: {ex.Message}" });
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
    }
}