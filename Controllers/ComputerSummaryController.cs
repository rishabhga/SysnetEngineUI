using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.UpdatesModels;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using ManageEngineWebApp.Helpers;

namespace ManageEngineWebApp.Controllers
{

    [AuthFilter]
    public class ComputerSummaryController : BaseController
    {
        public ComputerSummaryController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
            : base(httpClientFactory, configuration)
        {
        }

        [DynamicPermission("ComputerSummary.View", "View Dashboard")]
        public async Task<IActionResult> Deshboad(string? token = null, string? q = null, int locationId = 0, string? locationName = null, int groupid = 0, string? groupName = null, int comId = 0, string? companyName = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("locationId", out var lid) && int.TryParse(lid, out var locId)) locationId = locId;
                if (p.TryGetValue("locationName", out var ln)) locationName = ln;
                if (p.TryGetValue("groupid", out var gid) && int.TryParse(gid, out var grpId)) groupid = grpId;
                if (p.TryGetValue("groupName", out var gn)) groupName = gn;
                if (p.TryGetValue("comId", out var cid) && int.TryParse(cid, out var compId)) comId = compId;
                if (p.TryGetValue("companyName", out var cn)) companyName = cn;
            }
            else if (!string.IsNullOrEmpty(token))
            {
                var decrypted = ManageEngineWebApp.Helpers.EncryptionHelper.Decrypt(token);
                if (!string.IsNullOrEmpty(decrypted))
                {
                    try
                    {
                        var ctx = JsonConvert.DeserializeObject<ManageEngineWebApp.Controllers.HomeController.NavigationContext>(decrypted);
                        if (ctx != null)
                        {
                            locationId = ctx.LocationId ?? 0;
                            locationName = ctx.LocationName;
                            groupid = ctx.GroupId ?? 0;
                            groupName = ctx.GroupName;
                            comId = ctx.CompanyId ?? 0;
                            companyName = ctx.CompanyName;
                        }
                    }
                    catch { }
                }
            }

            if (!IsAuthorized(comId, groupid, locationId)) return RedirectToAction("Index", "Home");

            ViewBag.CompanyId = comId > 0 ? comId : (int?)null;
            ViewBag.GroupId = groupid > 0 ? groupid : (int?)null;
            ViewBag.LocationId = locationId > 0 ? locationId : (int?)null;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;
            ViewBag.companyid = comId;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationId;
            ViewBag.groupName = groupName;
            ViewBag.locationName = locationName;
            ViewBag.ApiBaseUrl = _baseUrl;

            var dalalist = new List<WindowsUserDetails>();
            var contectlist = new List<ConnectedClientDto>();
            List<string> activeComputers = new List<string>();

            try
            {
                var httpClient = GetClient();
                var query = BuildScopedQuery(comId == 0 ? null : comId, locationId == 0 ? null : locationId, groupid == 0 ? null : groupid);

                string userUrl = $"api/WindowsUserDetails/allUser{query}";
                string connectedUrl = $"api/Command/GetConnectedDevices";
                string companyUrl = $"api/CompaniesDetails/CompanyById?id={comId}";
                string vipUrl = "api/RamCpuDiskData/GetAllVIPs";
                var userTask = httpClient.GetAsync(userUrl);
                var connectedTask = httpClient.GetAsync(connectedUrl);
                var companyTask = httpClient.GetAsync(companyUrl);
                var vipTask = httpClient.GetAsync(vipUrl);

                await Task.WhenAll(userTask, connectedTask, companyTask, vipTask);

                var response = await userTask;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    if (data != null)
                    {
                        dalalist = data.Where(x => x != null && x.Status == "Enabled").ToList();
                        if (locationId > 0)
                            dalalist = dalalist.Where(x => x.LocationId == locationId).ToList();
                        else if (groupid > 0)
                            dalalist = dalalist.Where(x => x.GroupId == groupid).ToList();
                        else if (comId > 0)
                            dalalist = dalalist.Where(x => x.CompanyId == comId).ToList();
                    }
                }

                var response2 = await connectedTask;
                if (response2.IsSuccessStatusCode)
                {
                    var content2 = await response2.Content.ReadAsStringAsync();
                    contectlist = !string.IsNullOrEmpty(content2) ? JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content2) : null;

                    if (contectlist != null)
                    {
                        var authorizedComputerIds = dalalist.Where(d => !string.IsNullOrEmpty(d.DomainName)).Select(d => d.DomainName.ToUpper().Trim()).ToHashSet();
                        activeComputers = contectlist
                            .Where(d => d != null && !string.IsNullOrEmpty(d.UserName))
                            .Where(d => IsTopLevelAdmin() || authorizedComputerIds.Contains(d.UserName.ToUpper().Trim()))
                            .Select(d => d.UserName)
                            .ToList();
                    }
                }
                var companyResponse = await companyTask;
                if (companyResponse.IsSuccessStatusCode)
                {
                    var companyContent = await companyResponse.Content.ReadAsStringAsync();
                    var company = !string.IsNullOrEmpty(companyContent)
                        ? JsonConvert.DeserializeObject<dynamic>(companyContent)
                        : null;
                    if (company != null)
                    {
                        string logoUrl = company.logoUrl ?? company.LogoUrl;
                        ViewBag.CompanyLogoUrl = logoUrl;
                    }
                }

                var vipResponse = await vipTask;
                if (vipResponse.IsSuccessStatusCode)
                {
                    var vipContent = await vipResponse.Content.ReadAsStringAsync();
                    var vipData = !string.IsNullOrEmpty(vipContent)
                        ? JsonConvert.DeserializeObject<List<VIPClient>>(vipContent)
                        : new List<VIPClient>();
                    ViewBag.VipClients = vipData;
                }
                else
                {
                    ViewBag.VipClients = new List<VIPClient>();
                }
                string locationUrl = $"api/CompaniesDetails/Locationdata";
                var locationResponse = await httpClient.GetAsync(locationUrl);
                if (locationResponse.IsSuccessStatusCode)
                {
                    var locContent = await locationResponse.Content.ReadAsStringAsync();
                    var locs = JsonConvert.DeserializeObject<List<Locations>>(locContent);
                    var currentLoc = locs?.FirstOrDefault(l => l.Id == locationId);
                    ViewBag.IsLocationCritical = currentLoc?.IsCritical ?? false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deshboad Error: {ex.Message}");
            }

            if (contectlist != null)
            {
                ViewBag.ActiveComputers = activeComputers;
            }

            return View(dalalist);
        }
        [AuthFilter]
        [DynamicPermission("ComputerSummary.VIP", "View VIP Clients")]
        public IActionResult VIPClient(string? q = null, int comId = 0, int? groupId = null, int? locationId = null, string companyName = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("comId", out var cid) && int.TryParse(cid, out var c)) comId = c;
                if (p.TryGetValue("groupId", out var gid) && int.TryParse(gid, out var g)) groupId = g;
                if (p.TryGetValue("locationId", out var lid) && int.TryParse(lid, out var l)) locationId = l;
                if (p.TryGetValue("companyName", out var cn)) companyName = cn;
            }
            ViewBag.CompanyId = comId;
            ViewBag.GroupId = groupId;
            ViewBag.LocationId = locationId;
            ViewBag.CompanyName = companyName ?? "Unknown Company";

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDevices(int companyId, int groupId, int locationId)
        {
            if (!IsAuthorized(companyId, groupId, locationId)) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var httpClient = GetClient();
                var query = BuildScopedQuery(companyId, locationId, groupId);
                string url = $"api/WindowsUserDetails/allUser{query}";

                var response = await httpClient.GetAsync($"{_baseUrl}/{url}");
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content)
                        ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content)
                        : null;
                    var deviceList = (data ?? new List<WindowsUserDetails>())
                        .Where(x => x != null && (x.Status == "Enabled" || string.IsNullOrEmpty(x.Status)))
                        .Select(x => new
                        {
                            domainName = x.DomainName,
                            userName = x.UserName ?? "Unknown"
                        })
                        .OrderBy(x => x.domainName)
                        .ToList();

                    return Json(new { success = true, data = deviceList });
                }
                return Json(new { success = false, message = "Failed to fetch devices" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while fetching devices" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDevicesByCompany(int companyId)
        {
            if (!IsAuthorized(companyId)) return Json(new List<object>());

            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var query = BuildScopedQuery(companyId);
                var response = await client.GetAsync($"api/WindowsUserDetails/allUserByCompany{query}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    var deviceList = (data ?? new List<dynamic>()).Select(x => new
                    {
                        domainName = (string)(x.DomainName ?? x.domainName),
                        userName = (string)(x.UserName ?? x.userName)
                    }).ToList();
                    return Json(deviceList);
                }
                return Json(new List<object>());
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public async Task<IActionResult> GetDevicesByLocation(int companyId, int groupId, int locationId)
        {
            if (!IsAuthorized(companyId, groupId, locationId)) return Json(new List<object>());

            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var response = await client.GetAsync($"api/WindowsUserDetails/allUser{query}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    var deviceList = (data ?? new List<dynamic>()).Select(x => new
                    {
                        domainName = (string)(x.DomainName ?? x.domainName),
                        userName = (string)(x.UserName ?? x.userName)
                    }).ToList();
                    return Json(deviceList);
                }
                return Json(new List<object>());
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        public async Task<IActionResult> GetCriticalClients(int companyId, int? groupId, int? locationId)
        {
            if (!IsAuthorized(companyId, groupId, locationId)) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var client = GetClient();
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var response = await client.GetAsync($"{_baseUrl}/api/RamCpuDiskData/list{query}");

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var allCriticalClients = JsonConvert.DeserializeObject<List<VIPClient>>(content);
                    return Json(new { success = true, data = allCriticalClients });
                }

                return Json(new { success = false, message = "Failed to fetch critical clients" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCriticalClients Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddCriticalClient([FromBody] VIPClient criticalClient)
        {
            if (criticalClient == null) return Json(new { success = false, message = "Invalid client data" });
            if (!IsAuthorized(criticalClient.CompanyID, criticalClient.GroupsID, criticalClient.LocationID))
                return Json(new { success = false, message = "Unauthorized access to this scope." });

            if (string.IsNullOrEmpty(criticalClient.ClientId) || string.IsNullOrEmpty(criticalClient.ClientName))
                return Json(new { success = false, message = "Invalid client data" });

            try
            {
                var client = GetClient();
                var dto = new
                {
                    ClientId = criticalClient.ClientId,
                    ClientName = criticalClient.ClientName,
                    CompanyID = criticalClient.CompanyID,
                    GroupsID = criticalClient.GroupsID,
                    LocationID = criticalClient.LocationID,
                    CpuThreshold = criticalClient.CpuThreshold,
                    CpuWarningThreshold = criticalClient.CpuWarningThreshold,
                    CpuInfoThreshold = criticalClient.CpuInfoThreshold,
                    RamThreshold = criticalClient.RamThreshold,
                    RamWarningThreshold = criticalClient.RamWarningThreshold,
                    RamInfoThreshold = criticalClient.RamInfoThreshold,
                    DiskThreshold = criticalClient.DiskThreshold,
                    DiskWarningThreshold = criticalClient.DiskWarningThreshold,
                    DiskInfoThreshold = criticalClient.DiskInfoThreshold
                };
                string jsonData = JsonConvert.SerializeObject(dto);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_baseUrl}/api/RamCpuDiskData/add", content);

                if (response != null && response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Critical Client added successfully" });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to add critical client: {errorContent}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddCriticalClient Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCriticalClient([FromBody] string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return Json(new { success = false, message = "Invalid client ID" });

            if (!await IsDeviceAuthorized(clientId))
                return Json(new { success = false, message = "Unauthorized access to this device." });

            try
            {
                var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(clientId);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_baseUrl}/api/RamCpuDiskData/remove", content);

                if (response != null && response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Critical Client removed successfully" });
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to remove critical client: {errorContent}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoveCriticalClient Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(string machineId)
        {
            if (string.IsNullOrEmpty(machineId))
                return Json(new { success = false, error = "MachineId is required" });

            if (!await IsDeviceAuthorized(machineId))
                return Json(new { success = false, error = "Unauthorized" });

            try
            {
                var client = GetClient();
                string url = $"api/RamCpuDiskData/notifications/{Uri.EscapeDataString(machineId)}";

                var response = await client.GetAsync($"{_baseUrl}/{url}");
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return Json(new { success = false, error = "Failed to fetch notifications" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetNotifications Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsByLocation(int companyId, int? groupId, int? locationId)
        {
            // If locationId is 0 or null, we might want to fall back to general notifications
            if (locationId == null || locationId <= 0)
            {
                return RedirectToAction("GetRecentActivity");
            }

            if (!IsAuthorized(companyId, groupId, locationId))
            {
                // Return empty instead of error to avoid breaking the UI, but log it
                return Json(new List<object>());
            }

            try
            {
                var client = GetClient();
                // Ensure we pass parameters correctly to the API
                var queryParams = new List<string>();
                if (companyId > 0) queryParams.Add($"companyId={companyId}");
                if (groupId.HasValue && groupId > 0) queryParams.Add($"groupId={groupId}");
                if (locationId.HasValue && locationId > 0) queryParams.Add($"locationId={locationId}");

                string queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                string apiUrl = $"api/RamCpuDiskData/notifications/location{queryString}";

                var response = await client.GetAsync($"{_baseUrl}/{apiUrl}");
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetNotificationsByLocation Error: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationCriticalStatus(int locationId)
        {
            if (!IsAuthorized(null, null, locationId)) return Json(new { success = false, isCritical = false });

            try
            {
                var client = GetClient();
                string url = $"api/CompaniesDetails/Locationdata";

                var response = await client.GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var locations = JsonConvert.DeserializeObject<List<Locations>>(content);
                    var loc = locations?.FirstOrDefault(l => l.Id == locationId);
                    return Json(new { success = true, isCritical = loc?.IsCritical ?? false });
                }
                return Json(new { success = false, isCritical = false });
            }
            catch (Exception)
            {
                return Json(new { success = false, isCritical = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            if (id <= 0)
                return Json(new { success = false, error = "Invalid id" });

            try
            {
                var client = GetClient();
                string url = $"{_baseUrl}/api/RamCpuDiskData/notification/read/{id}";

                var response = await client.PostAsync(url, null);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Json(new { success = false, error = "Failed to mark as read" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MarkNotificationRead Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetRamCpuDiskData(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Json(new { status = "error", error = "Unauthorized" });

            if (string.IsNullOrEmpty(domain))
            {
                return Json(new { status = "error", error = "Domain is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(25);
                string url = $"{_baseUrl}/api/RamCpuDiskData/{Uri.EscapeDataString(domain)}";

                HttpResponseMessage response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response != null && response.IsSuccessStatusCode)
                {
                    dynamic root = JsonConvert.DeserializeObject<dynamic>(content);
                    var inner = root.data;

                    var formattedData = new
                    {
                        cpuUsage = (double)(inner.CPU ?? inner.cpu ?? 0),
                        ramUsage = (double)(inner.RAM ?? inner.ram ?? 0),
                        diskUsage = (double)(inner.Disk ?? inner.disk ?? 0)
                    };

                    return Json(new { status = "success", data = formattedData });
                }

                if (!string.IsNullOrEmpty(content))
                {
                    dynamic root = JsonConvert.DeserializeObject<dynamic>(content);
                    if (root?.status == "timeout")
                    {
                        return Json(new { status = "timeout" });
                    }
                }

                return Json(new { status = "error", error = "Failed to fetch data" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRamCpuDiskData Error: {ex.Message}");
                return Json(new { status = "error", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLastSeenTime(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(domain))
            {
                return Json(new { success = false, error = "Domain is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string url = $"{_baseUrl}/api/Client";

                HttpResponseMessage response = await client.GetAsync(url);

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var allConnections = JsonConvert.DeserializeObject<List<ClientConnection>>(content);

                    if (allConnections != null && allConnections.Any())
                    {
                        var deviceConnection = allConnections
                            .Where(x => x.ComputerName.Equals(domain, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.ConnectedAt)
                            .FirstOrDefault();

                        if (deviceConnection != null)
                        {
                            return Json(new
                            {
                                success = true,
                                lastSeen = deviceConnection.ConnectedAt,
                                lastSeenFormatted = deviceConnection.ConnectedAt.ToString("MM/dd/yyyy, hh:mm tt")
                            });
                        }
                    }

                    return Json(new { success = false, message = "No connection history found" });
                }

                return Json(new { success = false, error = "Failed to fetch data" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetLastSeenTime Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        [DynamicPermission("ComputerSummary.OTP", "Enable OTP Protection")]
        public async Task<IActionResult> EnableTempProtection(string machineId)
        {
            if (!await IsDeviceAuthorized(machineId)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                string url = $"{_baseUrl}/api/OtpVerification/OtpCode?Massage=ON&machineId={Uri.EscapeDataString(machineId)}";

                HttpResponseMessage response = await client.GetAsync(url);

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);

                    if (result != null)
                    {
                        if (result.status == "success")
                        {
                            await Task.Delay(2000);
                            var otpResponse = await client.GetAsync($"{_baseUrl}/api/OtpVerification/get-otp?machineId={Uri.EscapeDataString(machineId)}");
                            if (otpResponse.IsSuccessStatusCode)
                            {
                                var otpContent = await otpResponse.Content.ReadAsStringAsync();
                                var otpData = JsonConvert.DeserializeObject<dynamic>(otpContent);

                                if (otpData != null)
                                {
                                    return Json(new
                                    {
                                        success = true,
                                        message = "Temp protection enabled",
                                        otp = otpData.otp?.ToString(),
                                        expireAt = otpData.expireAt?.ToString(),
                                        isUsed = otpData.isUsed
                                    });
                                }
                            }

                            return Json(new { success = true, message = "Temp protection enabled" });
                        }
                        else if (result.status == "error")
                        {
                            return Json(new { success = false, error = result.msg?.ToString() ?? "Client not connected" });
                        }
                        if (result.otp != null)
                        {
                            return Json(new
                            {
                                success = true,
                                message = "Temp protection already enabled",
                                otp = result.otp?.ToString(),
                                expireAt = result.expireAt?.ToString(),
                                isUsed = result.isUsed
                            });
                        }
                    }

                    return Json(new { success = true, message = "Command sent successfully" });
                }
                else if (response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<dynamic>(content);
                    return Json(new { success = false, error = error?.msg?.ToString() ?? "Client not connected" });
                }

                return Json(new { success = false, error = "Failed to enable temp protection" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EnableTempProtection Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOtpCode(string machineId)
        {
            if (!await IsDeviceAuthorized(machineId)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string url = $"{_baseUrl}/api/OtpVerification/get-otp?machineId={Uri.EscapeDataString(machineId)}";

                HttpResponseMessage response = await client.GetAsync(url);

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Backend Response for {machineId}: {content}");

                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var otpData = JsonConvert.DeserializeObject<dynamic>(content);

                        if (otpData != null && otpData.otp != null)
                        {
                            DateTime? startAt = null;
                            DateTime? expireAt = null;

                            if (otpData.startAt != null)
                            {
                                DateTime.TryParse(otpData.startAt.ToString(), out DateTime startDt);
                                startAt = startDt;
                            }

                            if (otpData.expireAt != null)
                            {
                                DateTime.TryParse(otpData.expireAt.ToString(), out DateTime expireDt);
                                expireAt = expireDt;
                            }

                            bool isUsed = otpData.isUsed ?? false;
                            return Json(new
                            {
                                success = true,
                                otp = otpData.otp.ToString(),
                                machineId = otpData.machineId?.ToString(),
                                startAt = startAt?.ToString("o"),
                                expireAt = expireAt?.ToString("o"),
                                isUsed = isUsed
                            });
                        }
                    }

                    return Json(new { success = false, message = "No OTP found for this machine" });
                }

                return Json(new { success = false, error = "Failed to fetch OTP data" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOtpCode Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetTempProtectionStatus(string machineId)
        {
            if (!await IsDeviceAuthorized(machineId)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string otpUrl = $"{_baseUrl}/api/OtpVerification/OtpCode?Massage=ON&machineId={Uri.EscapeDataString(machineId)}";
                HttpResponseMessage response = await client.GetAsync(otpUrl);

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var otpResult = JsonConvert.DeserializeObject<dynamic>(content);

                    if (otpResult != null && otpResult.otp != null && otpResult.isUsed != null && !otpResult.isUsed)
                    {
                        var expireAt = otpResult.expireAt != null ? new DateTime((long)otpResult.expireAt) : DateTime.MinValue;
                        if (expireAt > DateTime.UtcNow)
                        {
                            return Json(new
                            {
                                success = true,
                                isActive = true,
                                hasOtp = true,
                                expireAt = expireAt
                            });
                        }
                    }
                }

                return Json(new { success = true, isActive = false, hasOtp = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTempProtectionStatus Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        [DynamicPermission("ComputerSummary.OTP", "Generate OTP")]
        public async Task<IActionResult> GenerateOtpCode(string machineId)
        {
            if (!await IsDeviceAuthorized(machineId)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string url = $"{_baseUrl}/api/OtpVerification/OtpGanrate?Massage=GENERATE&machineId={Uri.EscapeDataString(machineId)}";

                HttpResponseMessage response = await client.GetAsync(url);

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);

                    if (result != null && result.status == "success")
                    {
                        return Json(new
                        {
                            success = true,
                            status = "success",
                            otp = result.otp?.ToString(),
                            machineId = result.machineId?.ToString(),
                            expiry = result.expiry?.ToString(),
                            message = "OTP generated successfully"
                        });
                    }

                    return Json(new { success = false, error = "Failed to generate OTP" });
                }
                else if (response != null && response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var error = JsonConvert.DeserializeObject<dynamic>(content);
                    return Json(new { success = false, error = error?.msg?.ToString() ?? "Client not connected" });
                }

                return Json(new { success = false, error = "Failed to generate OTP" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GenerateOtpCode Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ClearOtpCode(string machineId)
        {
            if (!await IsDeviceAuthorized(machineId)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                string url = $"{_baseUrl}/api/OtpVerification/OtpCode?Massage=OFF&machineId={Uri.EscapeDataString(machineId)}";

                HttpResponseMessage response = await client.GetAsync(url);

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(content))
                    {
                        return Json(new { success = true, message = "Temp protection disabled" });
                    }

                    return Json(new { success = true, message = "OTP cleared" });
                }

                return Json(new { success = false, error = "Failed to clear OTP" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClearOtpCode Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }


        public async Task<IActionResult> BranchPatchMangnment(string? q = null, int companyid = 0, int groupid = 0, int locationid = 0,
            string? companyName = null, string? groupName = null, string? locationName = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("companyid", out var cid) && int.TryParse(cid, out var c)) companyid = c;
                if (p.TryGetValue("comId", out var cid2) && int.TryParse(cid2, out var c2)) companyid = c2;
                if (p.TryGetValue("groupid", out var gid) && int.TryParse(gid, out var g)) groupid = g;
                if (p.TryGetValue("locationid", out var lid) && int.TryParse(lid, out var l)) locationid = l;
                if (p.TryGetValue("locationId", out var lid2) && int.TryParse(lid2, out var l2)) locationid = l2;
                if (p.TryGetValue("companyName", out var cn)) companyName = cn;
                if (p.TryGetValue("groupName", out var gn)) groupName = gn;
                if (p.TryGetValue("locationName", out var ln)) locationName = ln;
            }
            if (!IsAuthorized(companyid, groupid, locationid)) return RedirectToAction("Index", "Home");
            ViewBag.CompanyId = companyid > 0 ? companyid : (int?)null;
            ViewBag.GroupId = groupid > 0 ? groupid : (int?)null;
            ViewBag.LocationId = locationid > 0 ? locationid : (int?)null;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;
            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.groupName = groupName;
            ViewBag.locationName = locationName;

            var localDatalist = new List<WindowsUserDetails>();
            var contectlist = new List<ConnectedClientDto>();
            List<string> activeComputers = new List<string>();
            try
            {
                var httpClient = GetClient();
                var query = BuildScopedQuery(companyid, locationid, groupid);
                string userUrl = $"api/WindowsUserDetails/allUser{query}";

                var userTask = httpClient.GetAsync($"{_baseUrl}/{userUrl}");
                var connectedTask = httpClient.GetAsync($"{_baseUrl}/api/Command/GetConnectedDevices");

                await Task.WhenAll(userTask, connectedTask);

                var response = await userTask;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    if (data != null)
                    {
                        localDatalist = data.Where(x => x.Status == "Enabled").ToList();
                        if (locationid > 0)
                            localDatalist = localDatalist.Where(x => x.LocationId == locationid).ToList();
                        else if (groupid > 0)
                            localDatalist = localDatalist.Where(x => x.GroupId == groupid).ToList();
                        else if (companyid > 0)
                            localDatalist = localDatalist.Where(x => x.CompanyId == companyid).ToList();
                    }
                }

                var response2 = await connectedTask;
                if (response2.IsSuccessStatusCode)
                {
                    var content2 = await response2.Content.ReadAsStringAsync();
                    contectlist = !string.IsNullOrEmpty(content2) ? JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content2) : null;
                    if (contectlist != null)
                    {
                        var authorizedComputerIds = localDatalist.Where(d => !string.IsNullOrEmpty(d.DomainName)).Select(d => d.DomainName.ToUpper().Trim()).ToHashSet();
                        activeComputers = contectlist
                            .Where(d => d != null && !string.IsNullOrEmpty(d.UserName))
                            .Where(d => IsTopLevelAdmin() || authorizedComputerIds.Contains(d.UserName.ToUpper().Trim()))
                            .Select(d => d.UserName)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BranchPatchMangnment Error: {ex.Message}");
            }

            ViewBag.ActiveComputers = activeComputers;
            return View(localDatalist);
        }

        [HttpPost]
        public async Task<IActionResult> ReScanPatches([FromBody] ReScanRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ClientId))
                return Json(new { success = false, error = "No clientId Provided." });

            try
            {
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(100);

                var content = new System.Net.Http.StringContent(
                    "\"ReScanWindowPatchUpdate\"",
                    System.Text.Encoding.UTF8,
                    "application/json");
                var response = await client.PostAsync(
                    $"{_baseUrl}/api/MultipleWindowThirdPartyPatchUpdate/ReScanPatchUpdate/{Uri.EscapeDataString(dto.ClientId)}",
                    content);

                var result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return Json(new { success = true, message = result });
                else
                    return Json(new { success = false, error = result });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReScanPatches Error: {ex.Message}");
                return Json(new { success = false, error = "Failed to send rescan command. Device may be offline." });
            }
        }



        public async Task<IActionResult> BranchPatchselection(string? q = null, int companyid = 0, int groupid = 0, int locationid = 0,
            string? selectedIds = null, string? domainids = null,
            string? companyName = null, string? groupName = null, string? locationName = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("companyid", out var cid) && int.TryParse(cid, out var c)) companyid = c;
                if (p.TryGetValue("comId", out var cid2) && int.TryParse(cid2, out var c2)) companyid = c2;
                if (p.TryGetValue("groupid", out var gid) && int.TryParse(gid, out var g)) groupid = g;
                if (p.TryGetValue("locationid", out var lid) && int.TryParse(lid, out var l)) locationid = l;
                if (p.TryGetValue("locationId", out var lid2) && int.TryParse(lid2, out var l2)) locationid = l2;
                if (p.TryGetValue("selectedIds", out var sid)) selectedIds = sid;
                if (p.TryGetValue("domainids", out var did)) domainids = did;
                if (p.TryGetValue("companyName", out var cn)) companyName = cn;
                if (p.TryGetValue("groupName", out var gn)) groupName = gn;
                if (p.TryGetValue("locationName", out var ln)) locationName = ln;
            }

            selectedIds ??= "";
            domainids ??= "";

            if (!IsAuthorized(companyid, groupid, locationid)) return RedirectToAction("Index", "Home");

            ViewBag.CompanyId = companyid > 0 ? companyid : (int?)null;
            ViewBag.GroupId = groupid > 0 ? groupid : (int?)null;
            ViewBag.LocationId = locationid > 0 ? locationid : (int?)null;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;
            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.groupName = groupName;
            ViewBag.locationName = locationName;
            ViewBag.selectedIds = selectedIds;
            ViewBag.domainids = domainids;

            var datalist = new List<PatchDetailsservice>();
            var repoList = new List<SoftwareRepoDetails>();

            try
            {
                var httpClient = GetClient();
                var query = BuildScopedQuery(companyid, locationid, groupid);
                string patchSeparator = query.Length > 0 ? "&" : "?";
                var patchTask = httpClient.GetAsync(
                    $"{_baseUrl}/api/MissingPatch{query}{patchSeparator}deviceIds={selectedIds}");
                var repoTask = httpClient.GetAsync($"{_baseUrl}/api/SoftwareRepoDetails");

                await Task.WhenAll(patchTask, repoTask);

                var response = await patchTask;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetailsservice>>(content) : null;
                    datalist = (data ?? new List<PatchDetailsservice>()).Where(x => x != null && x.UserCode != null).ToList();
                }

                var response2 = await repoTask;
                if (response2.IsSuccessStatusCode)
                {
                    var content2 = await response2.Content.ReadAsStringAsync();
                    repoList = JsonConvert.DeserializeObject<List<SoftwareRepoDetails>>(content2) ?? new List<SoftwareRepoDetails>();
                }

                foreach (var item in datalist)
                {
                    item.IsAvailableInRepo = repoList.Any(s =>
                    s.SoftwareName.Equals(item.PatchName, StringComparison.OrdinalIgnoreCase) &&
                    (s.Version == item.AvailableVersion
                    || s.Version == item.CurrentVersion
                    || string.IsNullOrEmpty(s.Version)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BranchPatchselection Error: {ex.Message}");
            }

            return View(datalist);
        }
        public async Task<IActionResult> BranchWinPatchselection(string? q = null, int companyid = 0, int groupid = 0, int locationid = 0,
            string? selectedIds = null, string? domainids = null,
            string? companyName = null, string? groupName = null, string? locationName = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("companyid", out var cid) && int.TryParse(cid, out var c)) companyid = c;
                if (p.TryGetValue("comId", out var cid2) && int.TryParse(cid2, out var c2)) companyid = c2;
                if (p.TryGetValue("groupid", out var gid) && int.TryParse(gid, out var g)) groupid = g;
                if (p.TryGetValue("locationid", out var lid) && int.TryParse(lid, out var l)) locationid = l;
                if (p.TryGetValue("locationId", out var lid2) && int.TryParse(lid2, out var l2)) locationid = l2;
                if (p.TryGetValue("selectedIds", out var sid)) selectedIds = sid;
                if (p.TryGetValue("domainids", out var did)) domainids = did;
                if (p.TryGetValue("companyName", out var cn)) companyName = cn;
                if (p.TryGetValue("groupName", out var gn)) groupName = gn;
                if (p.TryGetValue("locationName", out var ln)) locationName = ln;
            }

            selectedIds ??= "";
            domainids ??= "";

            if (!IsAuthorized(companyid, groupid, locationid)) return RedirectToAction("Index", "Home");

            ViewBag.CompanyId = companyid > 0 ? companyid : (int?)null;
            ViewBag.GroupId = groupid > 0 ? groupid : (int?)null;
            ViewBag.LocationId = locationid > 0 ? locationid : (int?)null;
            ViewBag.CompanyName = companyName;
            ViewBag.GroupName = groupName;
            ViewBag.LocationName = locationName;
            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.groupName = groupName;
            ViewBag.locationName = locationName;
            ViewBag.selectedIds = selectedIds;
            ViewBag.domainids = domainids;

            var datalist = new List<PatchDetail>();

            try
            {
                using var httpClient = GetClient();
                var query = BuildScopedQuery(companyid, locationid, groupid);
                string winSeparator = query.Length > 0 ? "&" : "?";
                var response = await httpClient.GetAsync(
                    $"{_baseUrl}/api/MissingPatch/windowpatch{query}{winSeparator}deviceIds={selectedIds}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetail>>(content) : null;
                    datalist = (data ?? new List<PatchDetail>()).Where(x => x.UserCode != null).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BranchWinPatchselection Error: {ex.Message}");
            }

            return View(datalist);
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePatch([FromBody] InstallRequest req, string domain)
        {
            return await PatchUpdate(req, domain);
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePatchselection([FromBody] UpdatePatchselectiondto updatePatchselectiondto)
        {
            if (!IsAuthorized(updatePatchselectiondto.companyid, updatePatchselectiondto.groupid, updatePatchselectiondto.locationid))
                return Json(new { success = false, error = "Unauthorized access to this scope." });

            try
            {
                if (string.IsNullOrEmpty(updatePatchselectiondto.deviceIds))
                    updatePatchselectiondto.deviceIds = updatePatchselectiondto.domainids;

                if (string.IsNullOrEmpty(updatePatchselectiondto.deviceIds))
                    return Json(new { success = false, error = "No device IDs were received. Please go back and re-select your devices." });

                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(120);
                string jsonData = JsonConvert.SerializeObject(updatePatchselectiondto);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(
                    $"{_baseUrl}/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchsethirdparty", content);

                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return Content(result, "application/json");
                    }
                    catch
                    {
                        return Json(new { success = true, message = result });
                    }
                }
                else
                {
                    return Json(new { success = false, error = result });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdatePatchselection Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatewinPatchselection([FromBody] UpdatewinPatchselectiondto updatewinPatchselectiondto)
        {
            if (!IsAuthorized(updatewinPatchselectiondto.companyid, updatewinPatchselectiondto.groupid, updatewinPatchselectiondto.locationid))
                return Json(new { success = false, error = "Unauthorized access to this scope." });

            try
            {
                if (string.IsNullOrEmpty(updatewinPatchselectiondto.deviceIds))
                    updatewinPatchselectiondto.deviceIds = updatewinPatchselectiondto.domainids;

                if (string.IsNullOrEmpty(updatewinPatchselectiondto.deviceIds))
                    return Json(new { success = false, error = "No device IDs were received. Please go back and re-select your devices." });
                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(120);
                string jsonData = JsonConvert.SerializeObject(updatewinPatchselectiondto);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(
                    $"{_baseUrl}/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchwindowpatch", content);

                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        return Content(result, "application/json");
                    }
                    catch
                    {
                        return Json(new { success = true, message = result });
                    }
                }
                else
                {
                    return Json(new { success = false, error = result });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdatewinPatchselection Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> deshboardupdate([FromBody] WindowsUserDetailsUpdateName windowsUserDetailsupdateName)
        {
            if (windowsUserDetailsupdateName.FullName == null) windowsUserDetailsupdateName.FullName = "";
            if (windowsUserDetailsupdateName.UserName == null) windowsUserDetailsupdateName.UserName = "";

            try
            {
                var client = GetClient();
                string jsonData = JsonConvert.SerializeObject(windowsUserDetailsupdateName);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("api/WindowsUserDetails/dashboardupdate", content);
                string result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "An internal server error occurred." });
            }
        }



        //public async Task<IActionResult> BranchPatchselection(int companyid, int groupid, int locationid, string selectedIds)
        //{
        //    // Convert selectedIds to list
        //    var idList = selectedIds.Split(',').Select(Int32.Parse).ToList();

        //    ViewBag.companyid = companyid;
        //    ViewBag.groupid = groupid;
        //    ViewBag.locationid = locationid;
        //    ViewBag.selectedIds = selectedIds;

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    HttpClientHandler handler1 = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    var datalist = new List<PatchDetailsservice>();
        //    var repoList = new List<SoftwareRepoDetails>();
        //    using (var httpClient = new HttpClient(handler))
        //    {

        //        // httpClient.BaseAddress = new Uri("https:// /api/MissingPatch");
        //        httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MissingPatch");

        //        var response = await httpClient.GetAsync("");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetailsservice>>(content) : null;
        //            datalist = (data ?? new List<PatchDetailsservice>()).Where(x => x != null && x.UserCode != null).ToList();
        //            //return View(datalist);
        //        }



        //        // ---------- API 2 : Software Repo ----------


        //        //return View(datalist);

        //    }

        //    using (var httpClient2 = new HttpClient(handler1))
        //    {
        //        httpClient2.BaseAddress = new Uri($"{_baseUrl}/api/SoftwareRepoDetails");

        //        var response1 = await httpClient2.GetAsync("");
        //        if (response1.IsSuccessStatusCode)
        //        {
        //            var content1 = await response1.Content.ReadAsStringAsync();
        //            repoList = JsonConvert.DeserializeObject<List<SoftwareRepoDetails>>(content1);
        //        }
        //    }
        //    // ----- Compare Logic -----
        //    // Match Patch name with software repo name
        //    foreach (var item in datalist)
        //    {
        //        item.IsAvailableInRepo = repoList.Any(s => s.Version == item.AvailableVersion);
        //    }

        //    return View(datalist);
        //}
        //public async Task<IActionResult> BranchWinPatchselection(int companyid, int groupid, int locationid, string selectedIds)
        //{
        //    // Convert selectedIds to list
        //    var idList = selectedIds.Split(',').Select(Int32.Parse).ToList();

        //    ViewBag.companyid = companyid;
        //    ViewBag.groupid = groupid;
        //    ViewBag.locationid = locationid;
        //    ViewBag.selectedIds = selectedIds;

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    HttpClientHandler handler1 = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    var datalist = new List<PatchDetail>();
        //    var repoList = new List<SoftwareRepoDetails>();
        //    using (var httpClient = new HttpClient(handler))
        //    {

        //        httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MissingPatch/windowpatch");

        //        var response = await httpClient.GetAsync("");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetail>>(content) : null;
        //            datalist = data.Where(x => x.UserCode != null).ToList();
        //            return View(datalist);
        //        }



        //        // ---------- API 2 : Software Repo ----------


        //        //return View(datalist);

        //    }

        //    //using (var httpClient2 = new HttpClient(handler1))
        //    //{

        //    //    var response1 = await httpClient2.GetAsync("");
        //    //    if (response1.IsSuccessStatusCode)
        //    //    {
        //    //        var content1 = await response1.Content.ReadAsStringAsync();
        //    //        repoList = JsonConvert.DeserializeObject<List<SoftwareRepoDetails>>(content1);
        //    //    }
        //    //}
        //    //// ----- Compare Logic -----
        //    //// Match Patch name with software repo name
        //    //foreach (var item in datalist)
        //    //{
        //    //    item.IsAvailableInRepo = repoList.Any(s => s.Version == item.AvailableVersion);
        //    //}

        //    return View(datalist);
        //}

        //public async Task<IActionResult> UpdatePatchselection(int companyid, int groupid, int locationid, string selectedIds, string domainids)
        //{
        //    var idList = selectedIds.Split(',').Select(int.Parse).ToList();



        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    UpdatePatchselectiondto updatePatchselectiondto = new UpdatePatchselectiondto();
        //    updatePatchselectiondto.companyid = companyid;
        //    updatePatchselectiondto.groupid = groupid;
        //    updatePatchselectiondto.locationid = locationid;
        //    updatePatchselectiondto.selectedIds = selectedIds;
        //    updatePatchselectiondto.domainids = domainids;

        //    using (HttpClient client = new HttpClient(handler))
        //    {

        //        client.BaseAddress = new Uri($"{_baseUrl}/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchsethirdparty");

        //        string jsonData = JsonConvert.SerializeObject(updatePatchselectiondto);
        //        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



        //        HttpResponseMessage response = await client.PostAsync("", content);

        //        string result = await response.Content.ReadAsStringAsync();
        //        if (response.IsSuccessStatusCode)
        //        {
        //            try
        //            {
        //                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
        //                return Json(jsonResponse);
        //            }
        //            catch
        //            {
        //                return Json(new { success = true, message = result });
        //            }
        //        }
        //        else
        //        {
        //            return Json(new { success = false, error = result });
        //        }
        //    }

        //    return Json(new { success = false, message = "Failed to process request" });
        //}

        ////UpdatewinPatchselection

        //public async Task<IActionResult> UpdatewinPatchselection(int companyid, int groupid, int locationid, string selectedIds, string domainids)
        //{

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    UpdatewinPatchselectiondto updatewinPatchselectiondto = new UpdatewinPatchselectiondto();
        //    updatewinPatchselectiondto.companyid = companyid;
        //    updatewinPatchselectiondto.groupid = groupid;
        //    updatewinPatchselectiondto.locationid = locationid;
        //    updatewinPatchselectiondto.selectedIds = selectedIds;
        //    updatewinPatchselectiondto.domainids = domainids;

        //    using (HttpClient client = new HttpClient(handler))
        //    {

        //        client.BaseAddress = new Uri($"{_baseUrl}/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchwindowpatch");

        //        string jsonData = JsonConvert.SerializeObject(updatewinPatchselectiondto);
        //        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



        //        HttpResponseMessage response = await client.PostAsync("", content);

        //        string result = await response.Content.ReadAsStringAsync();
        //        if (response.IsSuccessStatusCode)
        //        {
        //            try
        //            {
        //                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
        //                return Json(jsonResponse);
        //            }
        //            catch
        //            {
        //                return Json(new { success = true, message = result });
        //            }
        //        }
        //        else
        //        {
        //            return Json(new { success = false, error = result });
        //        }
        //    }

        //    return Json(new { success = false, message = "Failed to process request" });
        //}


        public List<UserDetails> datalist { get; set; }
        [AuthFilter(AllowedHierarchyLevel = 10, VerifyCompanyAccess = true)]
        public async Task<IActionResult> Index(string q = null, string domain = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                domain = ManageEngineWebApp.Helpers.EncryptionHelper.Decrypt(q);
            }

            if (string.IsNullOrEmpty(domain))
            {
                return RedirectToAction("Companies", "Companies");
            }

            try
            {
                using var httpClient = GetClient();
                var winUserResponse = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser");
                WindowsUserDetails winUser = null;
                if (winUserResponse.IsSuccessStatusCode)
                {
                    var winContent = await winUserResponse.Content.ReadAsStringAsync();
                    var winUsers = JsonConvert.DeserializeObject<List<WindowsUserDetails>>(winContent);
                    winUser = winUsers?.FirstOrDefault(x =>
                        (x.DomainName?.Equals(domain, StringComparison.OrdinalIgnoreCase) == true) ||
                        (x.UserCode?.Equals(domain, StringComparison.OrdinalIgnoreCase) == true));
                }
                using var detailsClient = GetClient();
                detailsClient.BaseAddress = new Uri($"{_baseUrl}/api/UserDetails");
                var response = await detailsClient.GetAsync("");
                ViewBag.ApiBaseUrl = _baseUrl;
                ViewBag.DisplayDomain = winUser?.DomainName ?? domain;
                ViewBag.UserCode = winUser?.UserCode ?? "N/A";
                ViewBag.LastLogUser = winUser?.UserName ?? "N/A";
                ViewBag.UserName = winUser?.UserCode ?? domain;
                ViewBag.DomainNameForCommand = winUser?.DomainName ?? domain;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<UserDetails>>(content);

                    if (data != null)
                    {
                        datalist = data.Where(x =>
                            (x.domainName?.Equals(domain, StringComparison.OrdinalIgnoreCase) == true) ||
                            (winUser != null && x.domainName?.Equals(winUser.DomainName, StringComparison.OrdinalIgnoreCase) == true)).ToList();

                        if (datalist.Any())
                        {
                            var details = datalist[0];
                            ViewBag.UserName = winUser?.UserCode ?? details.domainName;
                            ViewBag.DomainNameForCommand = winUser?.DomainName ?? details.domainName; ViewBag.DisplayDomain = winUser?.DomainName ?? details.domainName;
                            ViewBag.windowdetails = details.WindowName;
                            ViewBag.ip = details.IpAddress;
                            ViewBag.LastLogUser = !string.IsNullOrEmpty(winUser?.UserName) ? winUser.UserName : details.UserName;
                            ViewBag.scanTime = details.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            ViewBag.LastBootTime = details.LastBootTime;
                            ViewBag.primaryow = !string.IsNullOrEmpty(winUser?.FullName) ? winUser.FullName : details.PrimaryOwner;
                            ViewBag.UserFullName = winUser?.FullName;
                            ViewBag.UserCode = winUser?.UserCode ?? "N/A";
                        }
                        else if (winUser != null)
                        {
                            ViewBag.NoDevicesFoundInDetails = true;
                        }
                        else
                        {
                            ViewBag.NoDevices = true;
                        }
                    }
                }
                else
                {
                    ViewBag.ApiError = "Failed to fetch UserDetails";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.NoDevices = true;
            }
            return View();
        }



        //[HttpGet]
        //public async Task<IActionResult> GetDevicesByCompany(int companyId)
        //{
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    try
        //    {
        //        using (var httpClient = new HttpClient(handler))
        //        {
        //            // Try calling the UserDetails API which has all devices

        //            var response = await httpClient.GetAsync("");

        //            if (response.IsSuccessStatusCode)
        //            {
        //                var content = await response.Content.ReadAsStringAsync();

        //                if (!string.IsNullOrEmpty(content))
        //                {
        //                    var allDevices = JsonConvert.DeserializeObject<List<UserDetails>>(content);
        //                    if (allDevices != null && allDevices.Any())
        //                    {
        //                        // Filter devices by companyId if UserDetails has CompanyId field
        //                        // Or just return all devices
        //                        var deviceList = allDevices
        //                            .Select(x => new {
        //                                domainName = x.domainName,
        //                                userName = x.UserName ?? "Unknown"
        //                            })
        //                            .Where(x => !string.IsNullOrEmpty(x.domainName))
        //                            .Distinct()
        //                            .ToList();
        //                        return Json(deviceList);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[GetDevicesByCompany] Error: {ex.Message}");
        //    }
        //    return Json(new List<object>());
        //}


        public async Task<IActionResult> ConnectedClient()
        {
            var localDatalist = new List<ClientConnection>();
            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/Client");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    localDatalist = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ClientConnection>>(content) : new List<ClientConnection>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConnectedClient Error: {ex.Message}");
            }
            return View(localDatalist);
        }
        [DynamicPermission("ComputerSummary.RemoteAccess", "Execute Remote Command")]
        [HttpPost]
        public async Task<IActionResult> Comanddata(string domain)
        {
            using (var client = GetClient())
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/Command/" + domain);
                var content = new StringContent($"\"Scan\"", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                return Json(new { success = response.IsSuccessStatusCode, message = result });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BlockUsb(string domain)
        {
            using var httpClient = GetClient();
            var response = await httpClient.PostAsync($"{_baseUrl}/api/USbBlockingAndUnBlocking/block-usb/{domain}", null);
            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, message = await response.Content.ReadAsStringAsync() });
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUsb(string domain)
        {
            using var httpClient = GetClient();
            var response = await httpClient.PostAsync($"{_baseUrl}/api/USbBlockingAndUnBlocking/unblock-usb/{domain}", null);
            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, message = await response.Content.ReadAsStringAsync() });
        }

        public async Task<IActionResult> CheckScanResult(string domain)
        {
            using (var httpClient = GetClient())
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/SendScanStatus?domain={domain}");
                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    return Json(data);
                }
                return Json(new { status = "failed" });

            }
        }

        [DynamicPermission("ComputerSummary.RemoteAccess", "Remote Control")]
        public async Task<IActionResult> RemoteAccess(string domain)
        {
            using (var client = GetClient())
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/RemoteAccess/" + domain + "");
                var content = new StringContent($"\"{"Remote"}\"", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }
            return Redirect("Deshboad");

        }

        public async Task<IActionResult> Remotestatus(string domain)
        {
            using (var httpClient = GetClient())
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/SendScanStatus?domain={domain}");
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    return Json(data);
                }
                return Json(new { status = "failed" });

            }
        }
        public async Task<IActionResult> CheckAccessStatus(string domain)
        {
            using (var httpClient = GetClient())
            {
                string url = $"{_baseUrl}/api/RemoteAccess/CheckStatus?domain={domain}";
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        return Content(content, "application/json");
                    }
                }
                catch { }

                return Json(new { denied = false, accepted = false });
            }
        }
        public async Task<IActionResult> Live(string domain)
        {
            ViewBag.Domain = domain;
            using (var httpClient = GetClient())
            {
                string url = $"{_baseUrl}/api/RemoteAccess/monitor?domain={domain}";
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Client not found or no image received.";
                    return View();
                }
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<monitordata>(content);
                return View(data);
            }
        }


        public async Task<IActionResult> SendMouseMove(string domain, double x, double y)
        {
            var Mousedata = new MouseResponse
            {
                X = x,
                Y = y,
                Time = DateTime.Now,
            };
            using var client = GetClient();
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/RemoteAccess/MouseEvent/" + domain + "");
                string jsonData = JsonConvert.SerializeObject(Mousedata);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success" });
                }
                else
                {
                    return Json(new { status = "failed", message = result });
                }

            }
        }

        public async Task<IActionResult> SendLeftClick(string domain)
        {
            using var client = GetClient();
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/RemoteAccess/MouseLeftClick/" + domain + "");
                string jsonData = JsonConvert.SerializeObject("");
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success" });
                }
                else
                {
                    return Json(new { status = "failed", message = result });
                }
            }
        }

        public async Task<IActionResult> SendRightClick(string domain)
        {
            using var client = GetClient();
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/RemoteAccess/MouseRightClick/" + domain + "");
                string jsonData = JsonConvert.SerializeObject("");
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success" });
                }
                else
                {
                    return Json(new { status = "failed", message = result });
                }

            }

        }

        public async Task<IActionResult> SendKeyPress(string domain, string key)
        {
            using var client = GetClient();
            {


                client.BaseAddress = new Uri($"{_baseUrl}/api/RemoteAccess/KeyEvent?domain=" + domain + "&key=" + key + "");

                string jsonData = JsonConvert.SerializeObject("");
                var content = new StringContent($"\"{key}\"", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success" });
                }
                else
                {
                    return Json(new { status = "failed", message = result });
                }
            }
        }




        [DynamicPermission("ComputerSummary.RemoteAccess", "Stop Remote Session")]
        public async Task<IActionResult> Livestop(string domain)
        {
            try
            {
                using var client = GetClient();
                string url = $"{_baseUrl}/api/RemoteAccess/StopRemopte/{domain}";
                var content = new StringContent($"\"StopRemote\"", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);
                return Json(new { success = response.IsSuccessStatusCode });
            }
            catch (Exception)
            {
                return Json(new { success = false });
            }
        }
        public async Task<IActionResult> Remotemonitoring(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            ViewBag.Domain = domain;
            try
            {
                using var client = GetClient();
                string url = $"{_baseUrl}/api/RemoteAccess/monitor?domain={domain}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Client not found or no image received.";
                    return View();
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<monitordata>(content);
                return Json(data);
            }
            catch (Exception)
            {
                return Json(null);
            }
        }

        [HttpPost]
        [DynamicPermission("ComputerSummary.DeployPatch", "Install/Update Software")]
        public async Task<IActionResult> PatchUpdate([FromBody] InstallRequest req, string domain)
        {
            if (string.IsNullOrEmpty(domain) || req == null || string.IsNullOrEmpty(req.SoftwareName))
            {
                return Json(new { status = "failed", message = "Invalid request data" });
            }
            if (!await IsDeviceAuthorized(domain))
                return Json(new { status = "failed", message = "Unauthorized" });

            try
            {
                var patchUpdateRequest = new PatchUpdateRequest
                {
                    SoftwareName = req.SoftwareName,
                    DownloadUrl = $"{Request.Scheme}://{Request.Host}/SoftwareUpdates/{Uri.EscapeDataString(req.SoftwareName)}"
                };

                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                string jsonData = JsonConvert.SerializeObject(patchUpdateRequest);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{_baseUrl}/api/Command/update/{Uri.EscapeDataString(domain)}", content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success", message = "Install command sent successfully" });
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Json(new { status = "failed", message = "Device is not online. Command could not be delivered." });
                }
                return Json(new { status = "failed", message = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PatchUpdate Error: {ex.Message}");
                return Json(new { status = "failed", message = "Failed to send install command: " + ex.Message });
            }
        }

        [HttpPost]
        [DynamicPermission("ComputerSummary.Software", "Upload Software")]
        public async Task<IActionResult> UploadSoftware(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { status = "failed", message = "No file selected" });
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "SoftwareUpdates");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var safeName = Path.GetFileName(file.FileName);
            string filePath = Path.Combine(folderPath, safeName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new { status = "success", message = "Software uploaded successfully", fileName = safeName });
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return Json(new { status = "failed", message = "fileName missing" });

            using (HttpClient client = GetClient())
            {
                string apiUrl = $"{_baseUrl}/api/PatchDetails/DeleteSoftware/{fileName}";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, apiUrl);

                HttpResponseMessage response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success", message = result });
                }
                else
                {
                    return Json(new { status = "failed", message = result });
                }
            }
        }


        [HttpPost]
        [DynamicPermission("ComputerSummary.Software", "Uninstall Software")]
        public async Task<IActionResult> Uninstallsoftware([FromBody] UninstallRequest request, string domain)
        {
            if (string.IsNullOrEmpty(domain) || request == null || string.IsNullOrEmpty(request.SoftwareName))
            {
                return Json(new { status = "failed", message = "Invalid request data" });
            }
            if (!await IsDeviceAuthorized(domain))
                return Json(new { status = "failed", message = "Unauthorized" });

            try
            {
                var patchUpdateRequest = new PatchUpdateRequest
                {
                    SoftwareName = request.SoftwareName,
                    DownloadUrl = ""
                };

                using var client = GetClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                string jsonData = JsonConvert.SerializeObject(patchUpdateRequest);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(
                    $"{_baseUrl}/api/Command/softwareName/{Uri.EscapeDataString(domain)}", content);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success", message = "Uninstall command sent" });
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Json(new { status = "failed", message = "Device is not online. Command could not be delivered." });
                }
                return Json(new { status = "failed", message = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uninstallsoftware Error: {ex.Message}");
                return Json(new { status = "failed", message = "Failed to send uninstall command: " + ex.Message });
            }
        }
        public async Task<IActionResult> Uninstallsoftwarestatus(string softwareName, string domain)
        {
            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/Command/uninstallstatus?softwareName={Uri.EscapeDataString(softwareName)}&domain={Uri.EscapeDataString(domain)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    if (data != null) data.SoftwareName = softwareName;
                    return Json(data);
                }
                return Json(new { status = "failed", message = "API returned " + response.StatusCode });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uninstallsoftwarestatus Error: {ex.Message}");
                return Json(new { status = "failed", message = ex.Message });
            }
        }
        public async Task<IActionResult> installsoftwarestatus(string softwareName, string domain)
        {
            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/Command/installstatus?softwareName={Uri.EscapeDataString(softwareName)}&domain={Uri.EscapeDataString(domain)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    if (data != null) data.SoftwareName = softwareName;
                    return Json(data);
                }
                return Json(new { status = "failed", message = "API returned " + response.StatusCode });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"installsoftwarestatus Error: {ex.Message}");
                return Json(new { status = "failed", message = ex.Message });
            }
        }

        public async Task<IActionResult> SoftwareInstaller()
        {
            var datalist = new List<InstallerInfo>();
            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/Command/installers/");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    datalist = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstallerInfo>>(content) : null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SoftwareInstaller Error: {ex.Message}");
            }
            return View(datalist ?? new List<InstallerInfo>());
        }




        [HttpGet]
        public async Task<IActionResult> users(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<WindowsUserDetails>();
            try
            {
                using var httpClient = GetClient();
                string UCode = GetUCodeFromDomain(domain);

                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    localDatalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<WindowsUserDetails>();
                }
                var historyResponse = await httpClient.GetAsync($"{_baseUrl}/api/UserLogonHistory");
                if (historyResponse.IsSuccessStatusCode)
                {
                    var historyContent = await historyResponse.Content.ReadAsStringAsync();
                    var historyData = !string.IsNullOrEmpty(historyContent) ? JsonConvert.DeserializeObject<List<UserLogonHistory>>(historyContent) : null;
                    var userHistory = historyData?.Where(x => x.UserCode == UCode).ToList();

                    if (userHistory != null && userHistory.Any())
                    {
                        foreach (var user in localDatalist)
                        {
                            var latestLogin = userHistory
                                .Where(h => string.Equals(h.Username, user.UserName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(h.LogonTime))
                                .OrderByDescending(h => h.DateTime)
                                .FirstOrDefault();

                            user.LastLogin = latestLogin != null ? latestLogin.LogonTime : "Never";
                        }
                    }
                    else
                    {
                        foreach (var user in localDatalist)
                        {
                            user.LastLogin = "Never";
                        }
                    }
                }
                else
                {
                    foreach (var user in localDatalist)
                    {
                        user.LastLogin = "Never";
                    }
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        [DynamicPermission("ComputerSummary.View", "Device Summary")]
        public async Task<IActionResult> Summary(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<Summary>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Summary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<Summary>>(content);
                    localDatalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<Summary>();
                }
            }
            catch (Exception) { }

            if (!localDatalist.Any())
            {
                return Json(new { TotalHardware = 0, TotalSoftware = 0, CommercialSoftware = 0, NonCommercialSoftware = 0, ProhibitedSoftware = 0, MissingPatches = 0 });
            }

            var assetSummary = new
            {
                TotalHardware = localDatalist[0].TotalHardware,
                TotalSoftware = localDatalist[0].TotalSoftware,
                CommercialSoftware = localDatalist[0].CommercialSoftware,
                NonCommercialSoftware = localDatalist[0].NonCommercialSoftware,
                ProhibitedSoftware = localDatalist[0].ProhibitedSoftware,
                MissingPatches = localDatalist[0].MissingPatches
            };

            return Json(assetSummary);
        }

        [DynamicPermission("ComputerSummary.View", "OS Details")]
        public async Task<IActionResult> OSSummary(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<OSSummary>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/OSSummary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<OSSummary>>(content);
                    localDatalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<OSSummary>();
                }
            }
            catch (Exception) { }

            if (!localDatalist.Any())
            {
                return Json(new { OperatingSystem = "N/A", OSVersion = "N/A", RegisteredTo = "N/A", ProductID = "N/A", LicenseType = "N/A", SystemDrive = "N/A", OSCDKey = "N/A", OSServicePack = "N/A", OSBuildNumber = "N/A" });
            }

            var sosummary = new
            {
                OperatingSystem = localDatalist[0].OperatingSystem,
                OSVersion = localDatalist[0].OSVersion,
                RegisteredTo = localDatalist[0].RegisteredTo,
                ProductID = localDatalist[0].ProductID,
                LicenseType = localDatalist[0].LicenseType,
                SystemDrive = localDatalist[0].SystemDrive,
                OSCDKey = localDatalist[0].OSCDKey,
                OSServicePack = localDatalist[0].OSServicePack,
                OSBuildNumber = localDatalist[0].OSBuildNumber,
            };
            return Json(sosummary);
        }

        public async Task<IActionResult> DeviceSummary(string domain)
        {
            string UCode = GetUCodeFromDomain(domain);
            var localDatalist = new List<DeviceSummary>();
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/DeviceSummary");
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceSummary>>(content) : null;
                    localDatalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<DeviceSummary>();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { DeviceName = "N/A", Manufacturer = "N/A", Model = "N/A", SystemType = "N/A", SerialNumber = "N/A", Domain = "N/A", UserName = "N/A", TimeZone = "N/A", TotalPhysicalMemory = "N/A" });
                }

                var assetSummary = new
                {
                    DeviceManufacturer = localDatalist[0].DeviceManufacturer,
                    DeviceModel = localDatalist[0].DeviceModel,
                    DeviceType = localDatalist[0].DeviceType,
                    Processor = localDatalist[0].Processor,
                    Memory = localDatalist[0].Memory,
                    SerialNumber = localDatalist[0].SerialNumber,
                    ProcessorArchitecture = localDatalist[0].ProcessorArchitecture,
                    AssetTag = localDatalist[0].AssetTag,
                    UDID = localDatalist[0].UDID,
                    EASDeviceIdentifier = localDatalist[0].EASDeviceIdentifier,
                    BatteryLevel = localDatalist[0].BatteryLevel
                };

                return Json(assetSummary);
            }
            catch (Exception)
            {
                return Json(new { DeviceName = "N/A", Manufacturer = "N/A", Model = "N/A", SystemType = "N/A", SerialNumber = "N/A", Domain = "N/A", UserName = "N/A", TimeZone = "N/A", TotalPhysicalMemory = "N/A" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UsegeDisk(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            string UCode = GetUCodeFromDomain(domain);
            var localDatalist = new List<DiskUsage>();

            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/DiskUsage");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DiskUsage>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { success = false, message = "No disk data" });
                }

                var drivedata = new
                {
                    Drive = localDatalist[0].Drive,
                    TotalSpaceGB = localDatalist[0].TotalSpaceGB,
                    UsedSpaceGB = localDatalist[0].UsedSpaceGB,
                    FreeSpaceGB = localDatalist[0].FreeSpaceGB
                };

                return Ok(drivedata);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error fetching disk data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> services(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            string UCode = GetUCodeFromDomain(domain);
            var localDatalist = new List<WindowsService>();

            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/WindowsService");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsService>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        // Sends a service control command to the device via SignalR, then polls until the
        // device reports back a new record with the expected state change — same pattern as
        // AuditMemory/AuditProcessor so the UI gets a real success/fail based on what actually
        // happened on the device, not just whether the command was dispatched.
        [HttpPost]
        public async Task<IActionResult> ControlService(string domain, string serviceName, string action)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            if (string.IsNullOrWhiteSpace(serviceName))
                return Json(new { success = false, message = "Service name is required." });

            var allowed = new[] { "start", "stop", "restart" };
            if (!allowed.Contains(action?.ToLower()))
                return Json(new { success = false, message = $"Action '{action}' is not supported." });

            var cleanDomain = DeviceNameHelper.Normalize(domain);
            string UCode = GetUCodeFromDomain(domain);
            if (string.IsNullOrEmpty(cleanDomain) || string.IsNullOrEmpty(UCode))
                return Json(new { success = false, message = "Invalid device identifier." });

            try
            {
                using var httpClient = GetClient();

                // Step 1: capture baseline — current State and DateTime for this specific service
                string baselineState = null;
                DateTime? baselineDateTime = null;
                try
                {
                    var baseResp = await httpClient.GetAsync($"{_baseUrl}/api/WindowsService");
                    if (baseResp.IsSuccessStatusCode)
                    {
                        var baseContent = await baseResp.Content.ReadAsStringAsync();
                        var baseData = JsonConvert.DeserializeObject<List<WindowsService>>(baseContent);
                        var svc = baseData?
                            .Where(x => x.UserCode == UCode &&
                                   string.Equals(x.DisplayName, serviceName, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.DateTime)
                            .FirstOrDefault();
                        if (svc != null)
                        {
                            baselineState = svc.State;
                            baselineDateTime = svc.DateTime;
                        }
                    }
                }
                catch { }

                // Step 2: determine what state we expect after the action
                string expectedState = action.ToLower() switch
                {
                    "start" => "Running",
                    "stop" => "Stopped",
                    "restart" => "Running",
                    _ => null
                };

                // Step 3: send the command via SignalR
                var cmdUrl = $"{_baseUrl}/api/WindowsService/ServicesWorking" +
                             $"?clientId={Uri.EscapeDataString(cleanDomain)}" +
                             $"&Servicestype={Uri.EscapeDataString(action.ToLower())}" +
                             $"&ServiceName={Uri.EscapeDataString(serviceName)}";

                var cmdResp = await httpClient.PostAsync(cmdUrl, null);
                if (!cmdResp.IsSuccessStatusCode)
                {
                    var cmdErr = await cmdResp.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = !string.IsNullOrWhiteSpace(cmdErr) ? cmdErr : "Device not connected or command rejected." });
                }

                // Step 4: poll until the service reports a new DateTime (fresh scan) or
                // the State matches what we expect — up to 60 seconds (30 × 2s)
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        var checkResp = await httpClient.GetAsync($"{_baseUrl}/api/WindowsService");
                        if (!checkResp.IsSuccessStatusCode) continue;

                        var checkContent = await checkResp.Content.ReadAsStringAsync();
                        var checkData = JsonConvert.DeserializeObject<List<WindowsService>>(checkContent);
                        var svcNow = checkData?
                            .Where(x => x.UserCode == UCode &&
                                   string.Equals(x.DisplayName, serviceName, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(x => x.DateTime)
                            .FirstOrDefault();

                        if (svcNow == null) continue;

                        bool isNewer = !baselineDateTime.HasValue || svcNow.DateTime > baselineDateTime.Value;
                        bool stateMatch = expectedState == null ||
                                          string.Equals(svcNow.State, expectedState, StringComparison.OrdinalIgnoreCase);

                        if (isNewer && stateMatch)
                        {
                            return Json(new
                            {
                                success = true,
                                message = $"\"{serviceName}\" is now {svcNow.State}.",
                                newState = svcNow.State
                            });
                        }
                    }
                    catch { }
                }

                // Command was sent but device didn't report back in time
                return Json(new
                {
                    success = false,
                    message = $"Command sent but device hasn't confirmed the state change yet. The service may still be processing."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> groups(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<WindowsGroupDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/WindowsGroupDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<WindowsGroupDetails>>(content);
                    localDatalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<WindowsGroupDetails>();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [HttpGet]
        public async Task<IActionResult> drivers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/WindowDrivers");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<DeviceManagerItem>>(content);
                    var userDrivers = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<DeviceManagerItem>();

                    return Json(userDrivers);
                }
            }
            catch (Exception) { }
            return Json(new List<object>());
        }



        [HttpGet]
        public async Task<IActionResult> BIOS(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                var localDatalist = new List<BIOSDetails>();
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/BIOSDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<BIOSDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { Manufacturer = "N/A", Version = "N/A", SMBiosVersion = "N/A", ReleaseDate = "N/A", YearOfInstallation = "N/A", Status = "N/A", Description = "N/A", DateTime = DateTime.Now });
                }

                var biosdetaildata = new
                {
                    Manufacturer = localDatalist[0].Manufacturer,
                    Version = localDatalist[0].Version,
                    SMBiosVersion = localDatalist[0].SMBiosVersion,
                    ReleaseDate = localDatalist[0].ReleaseDate,
                    YearOfInstallation = localDatalist[0].YearOfInstallation,
                    Status = localDatalist[0].Status,
                    Description = localDatalist[0].Description,
                    DateTime = localDatalist[0].DateTime
                };

                return Json(biosdetaildata);
            }
            catch (Exception)
            {
                return Json(new { Manufacturer = "N/A", Version = "N/A", SMBiosVersion = "N/A", ReleaseDate = "N/A", YearOfInstallation = "N/A", Status = "N/A", Description = "N/A", DateTime = DateTime.Now });
            }
        }

        [HttpGet]
        public async Task<IActionResult> HardDisk(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<HardDiskDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/HardDiskDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<HardDiskDetails>>(content);
                    if (data != null)
                    {
                        localDatalist = data
                            .Where(x => x.UserCode == UCode)
                            .GroupBy(x => x.SerialNumber)
                            .Select(g => g.OrderByDescending(x => x.DateTime).First())
                            .OrderBy(x => x.Model)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HardDisk Error: {ex.Message}");
            }
            var result = localDatalist.Select(d => new
            {
                d.Id,
                d.Model,
                d.Manufacturer,
                d.SerialNumber,
                d.Description,
                TotalCapacity = Math.Round(d.TotalCapacity, 3),
                d.DeviceId,
                d.PowerOnHours,
                d.Temperature,
                d.Wear,
                d.ReadErrorsTotal,
                d.WriteErrorsTotal,
                d.ReadErrorsCorrected,
                d.UserCode,
                d.FirmwareVersion,
                d.InterfaceType,
                d.HealthStatus,
                d.PredictFailure,
                FreeSpaceGB = Math.Round(d.FreeSpaceGB, 3),
                UsedSpaceGB = Math.Round(d.UsedSpaceGB, 3),
                d.DateTime
            });

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> LocalDisk(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<LogicalDiskDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/LogicalDiskDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<LogicalDiskDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        [HttpGet]
        [DynamicPermission("ComputerSummary.View", "Computer Dashboard")]
        public async Task<IActionResult> Keyboard(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<KeyboardDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/KeyboardDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<KeyboardDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        public async Task<IActionResult> Monitor(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                var localDatalist = new List<MonitorInfo>();
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MonitorInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<MonitorInfo>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { Manufacturer = "N/A", MonitorType = "N/A", ScreenHeight = "N/A", ScreenWidth = "N/A", DeviceStatus = "N/A", Description = "N/A", SerialNumber = "N/A", InstalledWeek = "N/A", InstalledYear = "N/A", MonitorSize = "N/A", DateTime = DateTime.Now });
                }

                var monitordata1 = new
                {
                    Manufacturer = localDatalist[0].Manufacturer,
                    MonitorType = localDatalist[0].MonitorType,
                    ScreenHeight = localDatalist[0].ScreenHeight,
                    ScreenWidth = localDatalist[0].ScreenWidth,
                    DeviceStatus = localDatalist[0].DeviceStatus,
                    Description = localDatalist[0].Description,
                    SerialNumber = localDatalist[0].SerialNumber,
                    InstalledWeek = localDatalist[0].InstalledWeek,
                    InstalledYear = localDatalist[0].InstalledYear,
                    MonitorSize = localDatalist[0].MonitorSize,
                    DateTime = localDatalist[0].DateTime
                };
                return Json(monitordata1);
            }
            catch (Exception)
            {
                return Json(new { Manufacturer = "N/A", MonitorType = "N/A", ScreenHeight = "N/A", ScreenWidth = "N/A", DeviceStatus = "N/A", Description = "N/A", SerialNumber = "N/A", InstalledWeek = "N/A", InstalledYear = "N/A", MonitorSize = "N/A", DateTime = DateTime.Now });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Motherboard(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<MotherboardDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MotherboardDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<MotherboardDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        [HttpGet]
        public async Task<IActionResult> NetworkAdapters(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<NetworkAdapterDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/NetworkAdapterDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<NetworkAdapterDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [HttpGet]
        public async Task<IActionResult> PhysicalMemory(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                var localDatalist = new List<PhysicalMemoryDetails>();
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/PhysicalMemoryDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PhysicalMemoryDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { MaximumSupportedRAM = "N/A", Location = "N/A", SlotsAvailable = 0, SlotsUsed = 0 });
                }

                var physicalmemo = new
                {
                    MaximumSupportedRAM = localDatalist[0].MaximumSupportedRAM,
                    Location = localDatalist[0].Location,
                    SlotsAvailable = localDatalist[0].SlotsAvailable,
                    SlotsUsed = localDatalist[0].SlotsUsed
                };
                return Json(physicalmemo);
            }
            catch (Exception)
            {
                return Json(new { MaximumSupportedRAM = "N/A", Location = "N/A", SlotsAvailable = 0, SlotsUsed = 0 });
            }
        }


        [HttpGet]
        public async Task<IActionResult> MemorySlotDetails(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<MemorySlotDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MemorySlotDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MemorySlotDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }



        public async Task<IActionResult> PointingDevices(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<PointingDeviceInfo>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/PointingDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PointingDeviceInfo>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        [HttpGet]
        public async Task<IActionResult> Printers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<PrinterDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/PrinterDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PrinterDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        [HttpGet]
        public async Task<IActionResult> Processors(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            var empty = new ProcessorInfo { Name = "N/A", Manufacturer = "N/A", Status = "N/A", DateTime = DateTime.Now };

            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/ProcessorDetails");

                var response = await httpClient.GetAsync("");
                if (!response.IsSuccessStatusCode)
                {
                    return Json(empty);
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<ProcessorInfo>>(content);

                var localDatalist = data?.Where(x => x.UserCode == UCode)
                                          .OrderByDescending(x => x.DateTime)
                                          .ToList() ?? new List<ProcessorInfo>();

                if (!localDatalist.Any())
                {
                    return Json(empty);
                }
                return Json(localDatalist[0]);
            }
            catch (Exception)
            {
                return Json(empty);
            }
        }


        [HttpGet]
        public async Task<IActionResult> ProcessorHistory(string domain, int count = 20)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            var empty = new List<ProcessorInfo>();

            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}");

                var response = await httpClient.GetAsync($"api/ProcessorDetails/history/{UCode}?count={count}");
                if (!response.IsSuccessStatusCode)
                {
                    return Json(empty);
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<ProcessorInfo>>(content);

                return Json(data ?? empty);
            }
            catch (Exception)
            {
                return Json(empty);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MemorySummary(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            var empty = new MemorySummary { InstalledMemoryGB = 0, MaximumSupportedMemoryGB = 0, TotalSlots = 0, UsedSlots = 0, FreeMemoryGB = 0, UsedMemoryGB = 0, UsagePercent = 0, DateTime = DateTime.Now, MemoryModules = new List<PhysicalMemoryInfo>() };

            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}");

                var response = await httpClient.GetAsync($"api/MemorySlotDetails");
                if (!response.IsSuccessStatusCode)
                {
                    return Json(empty);
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<MemorySummary>>(content);

                var localDatalist = data?.Where(x => x.UserCode == UCode)
                                          .OrderByDescending(x => x.DateTime)
                                          .ToList() ?? new List<MemorySummary>();

                if (!localDatalist.Any())
                {
                    return Json(empty);
                }
                return Json(localDatalist[0]);
            }
            catch (Exception)
            {
                return Json(empty);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MemoryHistory(string domain, int count = 20)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            var empty = new List<MemorySummary>();

            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}");

                var response = await httpClient.GetAsync($"api/MemorySlotDetails/history/{UCode}?count={count}");
                if (!response.IsSuccessStatusCode)
                {
                    return Json(empty);
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<MemorySummary>>(content);

                return Json(data ?? empty);
            }
            catch (Exception)
            {
                return Json(empty);
            }
        }

        public async Task<IActionResult> Sound(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<SoundDeviceDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/SoundDeviceDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoundDeviceDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        public async Task<IActionResult> VideoControllers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<VideoDeviceInfo>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/VideoDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<VideoDeviceInfo>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        public async Task<IActionResult> USBControllers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<USBControllerInfo>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/USBControllerInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBControllerInfo>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        public async Task<IActionResult> USBHub(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<USBHubDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/USBHubDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBHubDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }



        [HttpGet]
        public async Task<IActionResult> DesktopApps(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<DesktopAppsModel>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/InstalledApplication/DesktopApps");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DesktopAppsModel>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [HttpPost]
        [DynamicPermission("ComputerSummary.Software", "Uninstall Software")]
        public async Task<IActionResult> Uninstall([FromBody] UninstallRequest request, string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            try
            {
                string softwareName = request.SoftwareName;
                string script = $"Get-WmiObject -Query \"SELECT * FROM Win32_Product WHERE Name = '{softwareName}'\" | ForEach-Object {{ $_.Uninstall() }}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{script}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    process.WaitForExit();
                }

                return Json(new { message = $"{softwareName} has been uninstalled." });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error uninstalling software: {"An internal server error occurred."}");
            }
        }

        [DynamicPermission("ComputerSummary.View", "Store Apps")]
        public async Task<IActionResult> MicrosoftstoreApps(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<MicrosoftStoreAppDetailsClass>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/InstalledApplication");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<MicrosoftStoreAppDetailsClass>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [DynamicPermission("ComputerSummary.View", "Metered Software")]
        public async Task<IActionResult> MeteredSoftware(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<InstalledApplication>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/InstalledApplication/MeteredSoftware");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<InstalledApplication>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        [DynamicPermission("ComputerSummary.View", "Installation Software")]
        public async Task<IActionResult> InstallationSoft(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<SoftwareFileModel>();
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/InstalledApplication/InstallationSoftlist?domain={domain}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoftwareFileModel>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //AntivirusDetails
        [DynamicPermission("ComputerSummary.View", "Antivirus Details")]
        public async Task<IActionResult> Antivirus(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<AntivirusDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        //patch universe update
        [DynamicPermission("ComputerSummary.View", "Missing Patches")]
        public async Task<IActionResult> Missingpatch(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<PatchDetailsservice>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MissingPatch");
                var response = await httpClient.GetAsync($"?userCode={UCode}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetailsservice>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x != null && x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [DynamicPermission("ComputerSummary.View", "Windows Missing Patches")]
        public async Task<IActionResult> Missingpatchwindow(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<PatchDetail>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/MissingPatch/windowpatch");
                var response = await httpClient.GetAsync($"?userCode={UCode}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetail>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x != null && x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        [DynamicPermission("ComputerSummary.View", "Installed Hotfixes")]
        public async Task<IActionResult> Hotfix(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<WindowsPatchInfo>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/ComputerDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    try
                    {
                        using var patchClient = GetClient();
                        patchClient.BaseAddress = new Uri($"{_baseUrl}/api/WindowsPatchInfo");
                        var patchResponse = await patchClient.GetAsync($"?userCode={UCode}");
                        if (patchResponse.IsSuccessStatusCode)
                        {
                            var patchContent = await patchResponse.Content.ReadAsStringAsync();
                            var patchData = !string.IsNullOrEmpty(patchContent) ? JsonConvert.DeserializeObject<List<WindowsPatchInfo>>(patchContent) : null;
                            if (patchData != null) localDatalist = patchData.Where(x => x.UserCode == UCode).ToList();
                        }
                    }
                    catch { }
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        [DynamicPermission("ComputerSummary.View", "Firewall Details")]
        public async Task<IActionResult> Firewall(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<AntivirusDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [DynamicPermission("ComputerSummary.View", "Missing Patches List")]
        public async Task<IActionResult> MissingPatches(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<PatchDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/PatchDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<PatchDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [HttpPost]
        [DynamicPermission("ComputerSummary.DeployPatch", "Install Patches")]
        public IActionResult UpdatePatches([FromBody] List<string> patches, string domain)
        {
            foreach (var patchId in patches)
            {
            }
            return Ok(new { message = "Update commands sent" });
        }




        [DynamicPermission("ComputerSummary.View", "Device Restrictions")]
        public async Task<IActionResult> RestrictionOnDevice(string domain)
        {
            var localDatalist = new List<DeviceRestrictionDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/DeviceRestrictionDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content)
                        ? JsonConvert.DeserializeObject<List<DeviceRestrictionDetails>>(content)
                        : null;
                    if (data != null)
                        localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new
                    {
                        IsCameraEnabled = "N/A",
                        IsTelemetryEnabled = "N/A",
                        CanModifyDateTime = "N/A",
                        IsBluetoothEnabled = "N/A"
                    });
                }

                var result = new
                {
                    IsCameraEnabled = localDatalist[0].IsCameraEnabled,
                    IsTelemetryEnabled = localDatalist[0].IsTelemetryEnabled,
                    CanModifyDateTime = localDatalist[0].CanModifyDateTime,
                    IsBluetoothEnabled = localDatalist[0].IsBluetoothEnabled
                };
                return Json(result);
            }
            catch (Exception)
            {
                return Json(new
                {
                    IsCameraEnabled = "N/A",
                    IsTelemetryEnabled = "N/A",
                    CanModifyDateTime = "N/A",
                    IsBluetoothEnabled = "N/A"
                });
            }
        }

        [DynamicPermission("ComputerSummary.View", "Network Restrictions")]
        public async Task<IActionResult> RestrictionOnNetwork(string domain)
        {
            var localDatalist = new List<RestrictionOnNetwork>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/RestrictionOnDevice/RestrictiononNetwork?domain={domain}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<RestrictionOnNetwork>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { InternetSharing = "N/A", VPN = "N/A", WiFi = "N/A", AllowWiFiConfiguration = "N/A", AutoConnectWiFiSense = "N/A" });
                }

                var RestricationNettailsfist = new
                {
                    InternetSharing = localDatalist[0].InternetSharing,
                    VPN = localDatalist[0].VPN,
                    WiFi = localDatalist[0].WiFi,
                    AllowWiFiConfiguration = localDatalist[0].AllowWiFiConfiguration,
                    AutoConnectWiFiSense = localDatalist[0].AutoConnectWiFiSense
                };
                return Json(RestricationNettailsfist);
            }
            catch (Exception)
            {
                return Json(new { InternetSharing = "N/A", VPN = "N/A", WiFi = "N/A", AllowWiFiConfiguration = "N/A", AutoConnectWiFiSense = "N/A" });
            }
        }
        public async Task<IActionResult> bluetootdetailsdata(string domain)
        {
            var localDatalist = new List<BluetoothDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/RestrictionOnDevice/BluetoothDetails?domain={domain}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BluetoothDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { Bluetooth = "N/A", Bluetoothdiscovery = "N/A", Bluetoothprepairing = "N/A", Bluetoothservicesadvertising = "N/A" });
                }

                var bluetootdetailslist = new
                {
                    Bluetooth = localDatalist[0].Bluetooth,
                    Bluetoothdiscovery = localDatalist[0].Bluetoothdiscovery,
                    Bluetoothprepairing = localDatalist[0].Bluetoothprepairing,
                    Bluetoothservicesadvertising = localDatalist[0].Bluetoothservicesadvertising
                };
                return Json(bluetootdetailslist);
            }
            catch (Exception)
            {
                return Json(new { Bluetooth = "N/A", Bluetoothdiscovery = "N/A", Bluetoothprepairing = "N/A", Bluetoothservicesadvertising = "N/A" });
            }
        }




        //SecurityPrivacyDetails
        public async Task<IActionResult> SecurityPrivacyDetails(string domain)
        {
            var localDatalist = new List<SecurityPrivacyDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/SecurityPrivacyDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SecurityPrivacyDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { LocationServices = "N/A", IsMicrosoftAccountConnected = "N/A", CanAddNonMicrosoftAccounts = "N/A", CanResetDevice = "N/A" });
                }

                var Security = new
                {
                    LocationServices = localDatalist[0].LocationServices,
                    IsMicrosoftAccountConnected = localDatalist[0].IsMicrosoftAccountConnected,
                    CanAddNonMicrosoftAccounts = localDatalist[0].CanAddNonMicrosoftAccounts,
                    CanResetDevice = localDatalist[0].CanResetDevice,
                };
                return Json(Security);
            }
            catch (Exception)
            {
                return Json(new { LocationServices = "N/A", IsMicrosoftAccountConnected = "N/A", CanAddNonMicrosoftAccounts = "N/A", CanResetDevice = "N/A" });
            }
        }


        //ApplicationSettings
        public async Task<IActionResult> ApplicationSettings(string domain)
        {
            var localDatalist = new List<ApplicationSettings>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/ApplicationSettings");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ApplicationSettings>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { InstallNonStoreApps = "N/A", InstallAppsOnlyInDeviceMemory = "N/A", StoreAppDataOnlyInDeviceMemory = "N/A", AutoUpdateStoreApps = "N/A" });
                }

                var application = new
                {
                    InstallNonStoreApps = localDatalist[0].InstallNonStoreApps,
                    InstallAppsOnlyInDeviceMemory = localDatalist[0].InstallAppsOnlyInDeviceMemory,
                    StoreAppDataOnlyInDeviceMemory = localDatalist[0].StoreAppDataOnlyInDeviceMemory,
                    AutoUpdateStoreApps = localDatalist[0].AutoUpdateStoreApps
                };
                return Json(application);
            }
            catch (Exception)
            {
                return Json(new { InstallNonStoreApps = "N/A", InstallAppsOnlyInDeviceMemory = "N/A", StoreAppDataOnlyInDeviceMemory = "N/A", AutoUpdateStoreApps = "N/A" });
            }
        }

        //SocialSearchSettings
        public async Task<IActionResult> SocialSearchSettings(string domain)
        {
            var localDatalist = new List<SocialSearchSettings>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/SocialSearchSettings");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SocialSearchSettings>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { CortanaEnabled = "N/A", SyncSettingsEnabled = "N/A", SearchLocationEnabled = "N/A" });
                }

                var social = new
                {
                    CortanaEnabled = localDatalist[0].CortanaEnabled,
                    SyncSettingsEnabled = localDatalist[0].SyncSettingsEnabled,
                    SearchLocationEnabled = localDatalist[0].SearchLocationEnabled
                };
                return Json(social);
            }
            catch (Exception)
            {
                return Json(new { CortanaEnabled = "N/A", SyncSettingsEnabled = "N/A", SearchLocationEnabled = "N/A" });
            }
        }



        //UsbAudit
        public async Task<IActionResult> UsbDeviceAudit(string domain)
        {
            var localDatalist = new List<USBDeviceInfo>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/UsbDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBDeviceInfo>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        //AuditHistory
        public async Task<IActionResult> AuditHistory(string domain)
        {
            var localDatalist = new List<UserAuditHistory>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/UserAuditHistory");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserAuditHistory>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        //LoginHistory
        public async Task<IActionResult> LoginHistory(string domain)
        {
            var localDatalist = new List<UserLogonHistory>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/UserLogonHistory");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserLogonHistory>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        // Update Log user
        public async Task<IActionResult> UpdateLoguser(string domain)
        {
            var localDatalist = new List<WindowsUserDetailsUpdates>();
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/UpdateLogs");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetailsUpdates>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [HttpGet]
        public async Task<IActionResult> GetBatteryHistory(string domain)
        {
            if (!await IsDeviceAuthorized(domain))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }
            string UCode = GetUCodeFromDomain(domain);
            string searchName = ManageEngineWebApp.Helpers.DeviceNameHelper.Normalize(domain);
            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/Battery/history/{Uri.EscapeDataString(searchName)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetBatteryHistory Error: {ex.Message}");
                return Json(new List<object>());
            }
        }

        //BatteryInfo
        [HttpGet]
        public async Task<IActionResult> Battery(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<BatteryInfo>();
            string UCode = GetUCodeFromDomain(domain);
            string searchName = ManageEngineWebApp.Helpers.DeviceNameHelper.Normalize(domain);
            try
            {
                using var httpClient = GetClient();

                var battResponse = await httpClient.GetAsync($"{_baseUrl}/api/Battery/history/{Uri.EscapeDataString(searchName)}");
                if (battResponse.IsSuccessStatusCode)
                {
                    var content = await battResponse.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<BatteryInfo>>(content);
                    if (data != null)
                        localDatalist = data
                            .OrderByDescending(x => x.ScanDate)
                            .ToList();
                }

                string wmiManufacturer = null;
                string wmiStatus = null;
                int wmiBatteryLevel = 0;
                string wmiSystemType = null;
                try
                {
                    var devResponse = await httpClient.GetAsync($"{_baseUrl}/api/DeviceSummary");
                    if (devResponse.IsSuccessStatusCode)
                    {
                        var devContent = await devResponse.Content.ReadAsStringAsync();
                        var devList = JsonConvert.DeserializeObject<List<dynamic>>(devContent);
                        var devRecord = devList?.FirstOrDefault(x =>
                            (string)x.UserCode == UCode || (string)x.userCode == UCode);
                        if (devRecord != null)
                        {
                            wmiManufacturer = (string)(devRecord.DeviceManufacturer ?? devRecord.deviceManufacturer);
                            wmiSystemType = (string)(devRecord.DeviceType ?? devRecord.deviceType);
                            string bl = (string)(devRecord.BatteryLevel ?? devRecord.batteryLevel ?? "");
                            if (!string.IsNullOrEmpty(bl))
                                int.TryParse(System.Text.RegularExpressions.Regex.Replace(bl, "[^0-9]", ""), out wmiBatteryLevel);
                        }
                    }
                }
                catch { }

                if (!localDatalist.Any())
                {
                    return Json(new
                    {
                        Manufacturer = wmiManufacturer ?? "Not found",
                        Status = wmiStatus ?? "Not found",
                        Description = "Not found",
                        BatteryLevel = wmiBatteryLevel > 0 ? wmiBatteryLevel.ToString() : "0",
                        BatteryPercentage = wmiBatteryLevel,
                        SystemType = wmiSystemType ?? "Not found",
                        UserCode = UCode,
                        DateTime = DateTime.Now,
                        BatteryName = (string)null,
                        SerialNumber = (string)null,
                        Chemistry = (string)null,
                        CycleCount = (int?)null,
                        BatteryHealthPercent = (decimal?)null,
                        WearLevelPercent = (decimal?)null,
                        WearRatePerMonth = (double?)null,
                        EstimatedRemainingMonths = (int?)null,
                        DesignCapacity = (long?)null,
                        FullChargeCapacity = (long?)null,
                        ScanDate = (DateTime?)null
                    });
                }

                var b = localDatalist[0];
                string manufacturer = !string.IsNullOrWhiteSpace(b.Manufacturer) ? b.Manufacturer : wmiManufacturer ?? "Not found";
                string status = !string.IsNullOrWhiteSpace(b.Status) ? b.Status : wmiStatus ?? "Not found";
                string description = !string.IsNullOrWhiteSpace(b.Description) ? b.Description : "Not found";
                int battPct = b.BatteryPercentage > 0 ? b.BatteryPercentage : wmiBatteryLevel;
                string systemType = !string.IsNullOrWhiteSpace(b.SystemType) ? b.SystemType : wmiSystemType ?? "Not found";


                List<CapacityHistoryEntryDto> capacityHistoryObj = null;
                List<UsageHistoryEntryDto> usageHistoryObj = null;
                List<BatteryUsageEntryDto> batteryUsageObj = null;

                try
                {
                    if (!string.IsNullOrWhiteSpace(b.CapacityHistoryJson))
                        capacityHistoryObj = JsonConvert.DeserializeObject<List<CapacityHistoryEntryDto>>(b.CapacityHistoryJson);
                }
                catch { }

                try
                {
                    if (!string.IsNullOrWhiteSpace(b.UsageHistoryJson))
                        usageHistoryObj = JsonConvert.DeserializeObject<List<UsageHistoryEntryDto>>(b.UsageHistoryJson);
                }
                catch { }

                try
                {
                    if (!string.IsNullOrWhiteSpace(b.BatteryUsageJson))
                        batteryUsageObj = JsonConvert.DeserializeObject<List<BatteryUsageEntryDto>>(b.BatteryUsageJson);
                }
                catch { }

                return Json(new
                {
                    Manufacturer = manufacturer,
                    Status = status,
                    Description = description,
                    BatteryLevel = battPct.ToString(),
                    BatteryPercentage = battPct,
                    LiveBatteryDetails = b.LiveBatteryDetails,
                    SystemType = systemType,
                    UserCode = b.UserCode,
                    DateTime = b.ScanDate,
                    IsCharging = b.IsCharging,
                    DesignCapacity = b.DesignCapacity,
                    FullChargeCapacity = b.FullChargeCapacity,
                    RemainingCapacity = b.RemainingCapacity,
                    CycleCount = b.CycleCount,
                    BatteryHealthPercent = b.BatteryHealthPercent,
                    WearLevelPercent = b.WearLevelPercent,
                    WearRatePerMonth = b.WearRatePerMonth,
                    EstimatedRemainingMonths = b.EstimatedRemainingMonths,
                    BatteryName = b.BatteryName,
                    SerialNumber = b.SerialNumber,
                    Chemistry = b.Chemistry,
                    ScanDate = b.ScanDate,
                    CapacityHistory = capacityHistoryObj,
                    UsageHistory = usageHistoryObj,
                    BatteryUsage = batteryUsageObj
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Battery GET] Error: {ex.Message}");
                return Json(new
                {
                    Manufacturer = "Not found",
                    Status = "Not found",
                    Description = "Not found",
                    BatteryLevel = "0",
                    BatteryPercentage = 0,
                    SystemType = "Not found",
                    UserCode = UCode,
                    DateTime = DateTime.Now
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AuditBattery([FromQuery] string domain)
        {
            if (!await IsDeviceAuthorized(domain))
                return Json(new { success = false, message = "Unauthorized access to this device." });
            try
            {
                var cleanDomain = DeviceNameHelper.Normalize(domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return Json(new { success = false, message = "Invalid device identifier." });

                using var httpClient = GetClient();

                string searchName = cleanDomain;
                DateTime? baselineTime = null;
                try
                {
                    var baseResp = await httpClient.GetAsync($"{_baseUrl}/api/Battery/history/{Uri.EscapeDataString(searchName)}");
                    if (baseResp.IsSuccessStatusCode)
                    {
                        var baseContent = await baseResp.Content.ReadAsStringAsync();
                        var baseData = JsonConvert.DeserializeObject<List<BatteryInfo>>(baseContent);
                        var latest = baseData?.OrderByDescending(x => x.ScanDate).FirstOrDefault();
                        if (latest != null)
                            baselineTime = latest.ScanDate;
                    }
                }
                catch { }

                var response = await httpClient.PostAsync(
                    $"{_baseUrl}/api/Battery/batteryFetchDetails?clientId={Uri.EscapeDataString(cleanDomain)}", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = !string.IsNullOrEmpty(errorContent) ? errorContent : $"Server returned {(int)response.StatusCode}" });
                }

                for (int i = 0; i < 40; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        var checkResp = await httpClient.GetAsync($"{_baseUrl}/api/Battery/history/{Uri.EscapeDataString(searchName)}");
                        if (checkResp.IsSuccessStatusCode)
                        {
                            var checkContent = await checkResp.Content.ReadAsStringAsync();
                            var checkData = JsonConvert.DeserializeObject<List<BatteryInfo>>(checkContent);
                            var latestNow = checkData?.OrderByDescending(x => x.ScanDate).FirstOrDefault();

                            if (latestNow != null)
                            {
                                DateTime currentTime = latestNow.ScanDate;
                                if (!baselineTime.HasValue || currentTime > baselineTime.Value)
                                {
                                    return Json(new { success = true, message = "Battery audit completed successfully.", data = new { metrics = latestNow } });
                                }
                            }
                        }
                    }
                    catch { }
                }
                return Json(new { success = false, message = "Live fetch timed out. Showing last known state." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewBatteryReport(string domain)
        {
            try
            {
                string cleanDomain = DeviceNameHelper.Normalize(domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return NotFound("Invalid device identifier.");

                using var httpClient = GetClient();

                var response = await httpClient.GetAsync($"{_baseUrl}/api/Battery/report/{Uri.EscapeDataString(cleanDomain)}");

                if (response.IsSuccessStatusCode)
                {
                    var htmlBytes = await response.Content.ReadAsByteArrayAsync();
                    return File(htmlBytes, "text/html");
                }

                return NotFound("Battery report not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving battery report.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> BatteryReportExists(string domain)
        {
            try
            {
                string cleanDomain = DeviceNameHelper.Normalize(domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return Json(new { exists = false });

                using var httpClient = GetClient();

                var response = await httpClient.GetAsync($"{_baseUrl}/api/Battery/report/{Uri.EscapeDataString(cleanDomain)}/exists");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Content(result, "application/json");
                }

                return Json(new { exists = false });
            }
            catch
            {
                return Json(new { exists = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LatestBatteryMetrics(string domain)
        {
            try
            {
                string cleanDomain = DeviceNameHelper.Normalize(domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return Ok(new { ready = false, message = "Invalid device identifier." });

                using var httpClient = GetClient();

                var response = await httpClient.GetAsync($"{_baseUrl}/api/Battery/metrics/{Uri.EscapeDataString(cleanDomain)}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return Content(result, "application/json");
                }

                return Ok(new { ready = false, message = "Metrics not found yet." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving battery metrics.");
            }
        }
        public async Task<IActionResult> BatteryLog(string domain)
        {
            var localDatalist = new List<BiosSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Batterylist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BiosSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //SummaryUpdateLog
        public async Task<IActionResult> SummaryUpdateLog(string domain)
        {
            var localDatalist = new List<SummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Summarydata/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        //OSSummarydata
        public async Task<IActionResult> OSSummaryUpdateLog(string domain)
        {
            var localDatalist = new List<OSSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/OSSummarydata/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }



        //DeviceSummaryChangeAuditUpdateLog
        public async Task<IActionResult> DeviceSummaryChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<DeviceSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/DeviceSummarylist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        //BiosSummaryChageUpdateLog
        public async Task<IActionResult> BiosSummaryChageUpdateLog(string domain)
        {
            var localDatalist = new List<BiosSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Bioslist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BiosSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        //HardDiskSummaryChangeAuditUpdateLog
        public async Task<IActionResult> HardDiskSummaryChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<HardDiskSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Harddisklist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<HardDiskSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //KeyboardSummaryChangeAuditUpdateLog
        public async Task<IActionResult> KeyboardSummaryChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<KeyboardSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Keyboardlist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<KeyboardSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //MonitorSummaryChangeAuditUpdateLog
        public async Task<IActionResult> MonitorSummaryChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<MonitorSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Monitorlist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MonitorSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //MotherboardSummaryChangeAuditUpdateLog
        public async Task<IActionResult> MotherboardSummaryChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<MotherboardSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/MotherboardSummaryChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MotherboardSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }




        //NetworkAdapterChangeAuditUpdateLog
        public async Task<IActionResult> NetworkAdapterChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<NetworkAdapterChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/Networkadhapterlist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<NetworkAdapterChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //ProcessorChangeAuditUpdateLog
        public async Task<IActionResult> ProcessorChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<ProcessorChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/ProcessorChangeAuditlist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ProcessorChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }


        //physicalMemoryDetailsChangeAudit
        public async Task<IActionResult> physicalMemoryDetailsChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<physicalMemoryDetailsChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/physicalMemoryDetailsChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<physicalMemoryDetailsChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //SoundDeviceChangeAuditUpdateLog
        public async Task<IActionResult> SoundDeviceChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<SoundDeviceChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/SoundDeviceChangeAuditlist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoundDeviceChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //USBControllerChangeAuditUpdateLog
        public async Task<IActionResult> USBControllerChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<OSSummaryChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/USBControllerChangeAuditlist/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummaryChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //WindowsUserChangeAudit
        public async Task<IActionResult> WindowsUserChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<WindowsUserChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/WindowsUserChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }
        //WindowsGroupChangeAudit
        public async Task<IActionResult> WindowsGroupChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<WindowsGroupChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/WindowsGroupChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsGroupChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //WindowDriversChangeAudit
        public async Task<IActionResult> WindowDriversChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<WindowDriversChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/WindowDriversChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDriversChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //DesktopAppsChangeAuditUpdateLog
        public async Task<IActionResult> DesktopAppsChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<DesktopAppsChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/DesktopAppsChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DesktopAppsChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //MSStoreAppChangeAudit
        public async Task<IActionResult> MSStoreAppChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<MSStoreAppChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/MSStoreAppChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MSStoreAppChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        //AntivirusChangeAudit
        public async Task<IActionResult> AntivirusChangeAuditUpdateLog(string domain)
        {
            var localDatalist = new List<AntivirusChangeAudit>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/TableChangesAudit/AntivirusChangeAudit/{UCode}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusChangeAudit>>(content) : null;
                    if (data != null) localDatalist = data;
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetailsJson(string domain)
        {
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/UserDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<UserDetails>>(content);
                    var userDetail = data?.FirstOrDefault(x => string.Equals(x.domainName, domain, StringComparison.OrdinalIgnoreCase));

                    if (userDetail != null)
                    {
                        return Json(new { success = true, ipAddress = userDetail.IpAddress ?? "N/A" });
                    }
                }
                return Json(new { success = false, message = "No data found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "An internal server error occurred." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetDiskUsageJson(string domain)
        {
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/DiskUsage");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<DiskUsage>>(content);
                    var diskData = data?.Where(x => x.UserCode == UCode)
                                        .OrderByDescending(x => x.DateTime)
                                        .FirstOrDefault();

                    if (diskData != null && diskData.TotalSpaceGB > 0)
                    {
                        double used = Convert.ToDouble(diskData.UsedSpaceGB);
                        double total = Convert.ToDouble(diskData.TotalSpaceGB);
                        double usagePercent = Math.Round((used / total) * 100.0, 0);

                        return Json(new { success = true, usagePercent = usagePercent });
                    }
                }
                return Json(new { success = false, message = "No disk data" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "An internal server error occurred." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetSummaryJson(string domain)
        {
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Summary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<Summary>>(content);
                    var summary = data?.FirstOrDefault(x => x.UserCode == UCode);

                    if (summary != null)
                    {
                        return Json(new
                        {
                            success = true,
                            cpuUsage = summary.TotalHardware,
                            ramUsage = summary.TotalSoftware
                        });
                    }
                }
                return Json(new { success = false, message = "No summary data" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "An internal server error occurred." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetConnectedDevices()
        {
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/GetConnectedDevices");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content)) return Json(new List<string>());

                    var data = JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content);
                    var connectedDomains = data?.Select(x => x.UserName).Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>();
                    return Json(connectedDomains);
                }
                return Json(new List<string>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetConnectedDevices Error: {ex.Message}");
                return Json(new List<string>());
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return Json(new List<object>());

            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails/chat-history?clientId={Uri.EscapeDataString(clientId)}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }

                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetChatHistory Error: {ex.Message}");
                return Json(new List<object>());
            }
        }
        [HttpGet]
        public IActionResult Notifications()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            if (!RoleHelper.HasPermission(HttpContext, "ComputerSummary.VIP"))
            {
                return Json(new { items = new List<object>(), count = 0 });
            }

            try
            {
                var companyId = RoleHelper.GetCompanyId(HttpContext) ?? 0;
                var groupId = RoleHelper.GetGroupId(HttpContext);
                var locationId = RoleHelper.GetLocationId(HttpContext);

                var client = GetClient();

                var queryParams = new List<string>();
                if (companyId > 0) queryParams.Add($"companyId={companyId}");
                if (groupId != null && groupId > 0) queryParams.Add($"groupId={groupId}");
                if (locationId != null && locationId > 0) queryParams.Add($"locationId={locationId}");

                string url = "api/RamCpuDiskData/notifications/location";
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var response = await client.GetAsync($"{_baseUrl}/{url}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var notifications = JsonConvert.DeserializeObject<List<dynamic>>(content) ?? new List<dynamic>();
                    return Json(new { items = notifications, count = notifications.Count });
                }
                return Json(new { items = new List<object>(), count = 0 });
            }
            catch
            {
                return Json(new { items = new List<object>(), count = 0 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AuditMemory([FromQuery] string domain, [FromQuery] string hostName = null)
        {
            try
            {
                var cleanDomain = DeviceNameHelper.Normalize(!string.IsNullOrWhiteSpace(hostName) ? hostName : domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return Json(new { success = false, message = "Invalid device identifier." });

                string UCode = domain;
                using var httpClient = GetClient();

                DateTime? baselineTime = null;
                try
                {
                    var baseResp = await httpClient.GetAsync($"{_baseUrl}/api/MemorySlotDetails");
                    if (baseResp.IsSuccessStatusCode)
                    {
                        var baseContent = await baseResp.Content.ReadAsStringAsync();
                        var baseData = JsonConvert.DeserializeObject<List<MemorySummary>>(baseContent);
                        var latest = baseData?.FirstOrDefault(x => x.UserCode == UCode);
                        if (latest != null)
                            baselineTime = latest.DateTime;
                    }
                }
                catch { }

                // Send the rescan command
                var response = await httpClient.PostAsync(
                    $"{_baseUrl}/api/PhysicalMemoryDetails/MemoryRescan?clientId={Uri.EscapeDataString(cleanDomain)}", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = !string.IsNullOrEmpty(errorContent) ? errorContent : "Device not connected or rescan failed." });
                }

                for (int i = 0; i < 90; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        var checkResp = await httpClient.GetAsync($"{_baseUrl}/api/MemorySlotDetails");
                        if (checkResp.IsSuccessStatusCode)
                        {
                            var checkContent = await checkResp.Content.ReadAsStringAsync();
                            var checkData = JsonConvert.DeserializeObject<List<MemorySummary>>(checkContent);
                            var latestNow = checkData?.FirstOrDefault(x => x.UserCode == UCode);
                            if (latestNow != null)
                            {
                                DateTime currentTime = latestNow.DateTime;
                                if (!baselineTime.HasValue || currentTime > baselineTime.Value)
                                {
                                    return Json(new { success = true, message = "Memory audit completed. Fresh data received!", data = new { metrics = latestNow } });
                                }
                            }
                        }
                    }
                    catch { }
                }
                return Json(new { success = false, message = "The device did not report fresh data in time. It may still be processing — try auditing again shortly." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AuditProcessor([FromQuery] string domain, [FromQuery] string hostName = null)
        {
            try
            {
                var cleanDomain = DeviceNameHelper.Normalize(!string.IsNullOrWhiteSpace(hostName) ? hostName : domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return Json(new { success = false, message = "Invalid device identifier." });

                string UCode = domain;
                using var httpClient = GetClient();
                DateTime? baselineTime = null;
                try
                {
                    var baseResp = await httpClient.GetAsync($"{_baseUrl}/api/ProcessorDetails/history/{Uri.EscapeDataString(UCode)}?count=1");
                    if (baseResp.IsSuccessStatusCode)
                    {
                        var baseContent = await baseResp.Content.ReadAsStringAsync();
                        var baseData = JsonConvert.DeserializeObject<List<dynamic>>(baseContent);
                        var latest = baseData?.LastOrDefault();
                        if (latest != null)
                            baselineTime = (DateTime?)(latest.DateTime ?? latest.dateTime);
                    }
                }
                catch { }

                var response = await httpClient.PostAsync(
                    $"{_baseUrl}/api/ProcessorDetails/ProcessorRescan?clientId={Uri.EscapeDataString(cleanDomain)}", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = !string.IsNullOrEmpty(errorContent) ? errorContent : "Device not connected or rescan failed." });
                }

                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        var checkResp = await httpClient.GetAsync($"{_baseUrl}/api/ProcessorDetails/history/{Uri.EscapeDataString(UCode)}?count=1");
                        if (checkResp.IsSuccessStatusCode)
                        {
                            var checkContent = await checkResp.Content.ReadAsStringAsync();
                            var checkData = JsonConvert.DeserializeObject<List<dynamic>>(checkContent);
                            var latestNow = checkData?.LastOrDefault();
                            if (latestNow != null)
                            {
                                DateTime? currentTime = (DateTime?)(latestNow.DateTime ?? latestNow.dateTime);
                                if (currentTime.HasValue && (!baselineTime.HasValue || currentTime.Value > baselineTime.Value))
                                {
                                    return Json(new { success = true, message = "Processor audit completed. Fresh data received!" });
                                }
                            }
                        }
                    }
                    catch { }
                }
                return Json(new { success = false, message = "Processor audit timed out. The device may still be processing. Try refreshing." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AuditHardDisk([FromQuery] string domain, [FromQuery] string hostName = null)
        {
            try
            {
                var cleanDomain = DeviceNameHelper.Normalize(!string.IsNullOrWhiteSpace(hostName) ? hostName : domain);
                if (string.IsNullOrEmpty(cleanDomain))
                    return Json(new { success = false, message = "Invalid device identifier." });

                string UCode = domain;
                using var httpClient = GetClient();

                DateTime? baselineTime = null;
                try
                {
                    var baseResp = await httpClient.GetAsync($"{_baseUrl}/api/HardDiskDetails/history?userCode={Uri.EscapeDataString(UCode)}&take=1");
                    if (baseResp.IsSuccessStatusCode)
                    {
                        var baseContent = await baseResp.Content.ReadAsStringAsync();
                        var baseData = JsonConvert.DeserializeObject<List<dynamic>>(baseContent);
                        var latest = baseData?.FirstOrDefault();
                        if (latest != null)
                            baselineTime = (DateTime?)(latest.DateTime ?? latest.dateTime);
                    }
                }
                catch { }

                var response = await httpClient.PostAsync(
                    $"{_baseUrl}/api/HardDiskDetails/HarddiskRescan?clientId={Uri.EscapeDataString(cleanDomain)}", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = !string.IsNullOrEmpty(errorContent) ? errorContent : "Device not connected or rescan failed." });
                }

                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        var checkResp = await httpClient.GetAsync($"{_baseUrl}/api/HardDiskDetails/history?userCode={Uri.EscapeDataString(UCode)}&take=1");
                        if (checkResp.IsSuccessStatusCode)
                        {
                            var checkContent = await checkResp.Content.ReadAsStringAsync();
                            var checkData = JsonConvert.DeserializeObject<List<dynamic>>(checkContent);
                            var latestNow = checkData?.FirstOrDefault();
                            if (latestNow != null)
                            {
                                DateTime? currentTime = (DateTime?)(latestNow.DateTime ?? latestNow.dateTime);
                                if (currentTime.HasValue && (!baselineTime.HasValue || currentTime.Value > baselineTime.Value))
                                {
                                    return Json(new { success = true, message = "Hard Disk audit completed. Fresh data received!" });
                                }
                            }
                        }
                    }
                    catch { }
                }
                return Json(new { success = false, message = "Hard Disk audit timed out. The device may still be processing. Try refreshing." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCriticalSettings([FromBody] VIPClient settings)
        {
            using var client = GetClient();
            var response = await client.PostAsJsonAsync($"{_baseUrl}/api/RamCpuDiskData/UpdateCriticalSettings", settings);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Failed to update VIP settings." });
        }
    }
    public class CapacityHistoryEntryDto
    {
        public string Period { get; set; }
        public int FullChargeCapacity { get; set; }
        public int DesignCapacity { get; set; }
    }

    public class UsageHistoryEntryDto
    {
        public string Period { get; set; }
        public string BatteryActive { get; set; }
        public string AcActive { get; set; }
    }

    public class BatteryUsageEntryDto
    {
        public string StartTime { get; set; }
        public string State { get; set; }
        public string Duration { get; set; }
        public string EnergyDrained { get; set; }
    }
}