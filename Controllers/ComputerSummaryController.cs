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
        public async Task<IActionResult> Deshboad(int locationId, string locationName, int groupid, string groupName, int comId, string companyName)
        {
            if (!IsAuthorized(comId, groupid, locationId)) return RedirectToAction("Index", "Home");

            ViewBag.CompanyName = companyName;
            ViewBag.groupName = groupName;
            ViewBag.locationName = locationName;
            ViewBag.companyid = comId;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationId;
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
                var userTask = httpClient.GetAsync(userUrl);
                var connectedTask = httpClient.GetAsync(connectedUrl);
                var companyTask = httpClient.GetAsync(companyUrl);

                await Task.WhenAll(userTask, connectedTask, companyTask);

                var response = await userTask;
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    if (data != null)
                    {
                        dalalist = data.Where(x => x != null && x.Status == "Enabled").ToList();
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
            catch (Exception)
            {
            }

            if (contectlist != null)
            {
                ViewBag.ActiveComputers = activeComputers;
            }

            return View(dalalist);
        }
        [AuthFilter]
        [DynamicPermission("ComputerSummary.VIP", "View VIP Clients")]
        public IActionResult VIPClient(int comId, int? groupId, int? locationId, string companyName)
        {
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

                var response = await httpClient.GetAsync(url);
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
                var response = await client.GetAsync($"api/RamCpuDiskData/list{query}");

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
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var dto = new
                {
                    ClientId = criticalClient.ClientId,
                    ClientName = criticalClient.ClientName,
                    CompanyID = criticalClient.CompanyID,
                    GroupsID = criticalClient.GroupsID,
                    LocationID = criticalClient.LocationID
                };
                string jsonData = JsonConvert.SerializeObject(dto);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/RamCpuDiskData/add", content);

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
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                string jsonData = JsonConvert.SerializeObject(clientId);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/RamCpuDiskData/remove", content);

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
            if (!await IsDeviceAuthorized(machineId)) return Json(new { success = false, error = "Unauthorized" });

            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is Required" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                string url = $"api/RamCpuDiskData/notifications/{Uri.EscapeDataString(machineId)}";

                var response = await client.GetAsync(url);
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
            if (!IsAuthorized(companyId, groupId, locationId)) return Json(new { success = false, error = "Unauthorized" });

            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var query = BuildScopedQuery(companyId, locationId, groupId);
                string url = $"api/RamCpuDiskData/notifications/location{query}";

                var response = await client.GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return Json(new { success = false, error = "Failed to fetch location notifications" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
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
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                string url = $"api/RamCpuDiskData/notification/read/{id}";

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
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    dynamic root = JsonConvert.DeserializeObject<dynamic>(content);
                    var inner = root.data;

                    var formattedData = new
                    {
                        cpuUsage = (double)inner.cpu,
                        ramUsage = (double)inner.ram,
                        diskUsage = (double)inner.disk
                    };

                    return Json(new { status = "success", data = formattedData });
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


        public async Task<IActionResult> BranchPatchMangnment(int companyid, int groupid, int locationid)
        {
            if (!IsAuthorized(companyid, groupid, locationid)) return RedirectToAction("Index", "Home");

            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;

            var localDatalist = new List<WindowsUserDetails>();
            var contectlist = new List<ConnectedClientDto>();
            List<string> activeComputers = new List<string>();
                try
                {
                    var httpClient = GetClient();
                    var query = BuildScopedQuery(companyid, locationid, groupid);
                    string userUrl = $"api/WindowsUserDetails/allUser{query}";

                    var userTask = httpClient.GetAsync(userUrl);
                    var connectedTask = httpClient.GetAsync("api/Command/GetConnectedDevices");

                    await Task.WhenAll(userTask, connectedTask);

                    var response = await userTask;
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                        if (data != null)
                        {
                            localDatalist = data.Where(x => x.Status == "Enabled").ToList();
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
                catch (Exception) { }

                ViewBag.ActiveComputers = activeComputers;
                return View(localDatalist);
            }
        
        public async Task<IActionResult> BranchPatchselection(int companyid, int groupid, int locationid, string selectedIds, string domainids)
        {
            if (!IsAuthorized(companyid, groupid, locationid)) return RedirectToAction("Index", "Home");

            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.selectedIds = selectedIds;
            ViewBag.domainids = domainids;

            var datalist = new List<PatchDetailsservice>();
            var repoList = new List<SoftwareRepoDetails>();

            try
            {
                var httpClient = GetClient();
                var query = BuildScopedQuery(companyid, locationid, groupid);
                // Append deviceIds manually as they are feature-specific
                var patchTask = httpClient.GetAsync($"api/MissingPatch{query}&deviceIds={selectedIds}");
                var repoTask = httpClient.GetAsync("api/SoftwareRepoDetails");

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
                        s.Version == item.AvailableVersion &&
                        s.SoftwareName.Equals(item.PatchName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception)
            {
            }

            return View(datalist);
        }
        public async Task<IActionResult> BranchWinPatchselection(int companyid, int groupid, int locationid, string selectedIds, string domainids)
        {
            if (!IsAuthorized(companyid, groupid, locationid)) return RedirectToAction("Index", "Home");

            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.selectedIds = selectedIds;
            ViewBag.domainids = domainids;

            var datalist = new List<PatchDetail>();

            try
            {
                using var httpClient = GetClient();
                var query = BuildScopedQuery(companyid, locationid, groupid);
                var response = await httpClient.GetAsync($"{_baseUrl}/api/MissingPatch/windowpatch{query}&deviceIds={selectedIds}");
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
                        var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                        return Json(jsonResponse);
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
                        var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                        return Json(jsonResponse);
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
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
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
        public async Task<IActionResult> Index(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return RedirectToAction("Companies", "Companies");
            }
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/UserDetails");
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserDetails>>(content) : null;

                    if (data != null)
                    {
                        datalist = data.Where(x => x.domainName == domain).ToList();

                        if (datalist.Any())
                        {
                            ViewBag.UserName = datalist[0].domainName;
                            ViewBag.windowdetails = datalist[0].WindowName;
                            ViewBag.ip = datalist[0].IpAddress;
                            ViewBag.LastLogUser = datalist[0].UserName;
                            ViewBag.LastBootTime = datalist[0].LastBootTime;
                            ViewBag.scanTime = datalist[0].DateTime;
                            ViewBag.primaryow = datalist[0].PrimaryOwner;
                        }
                        else
                        {
                            ViewBag.NoDevices = true;
                        }
                    }
                    else
                    {
                        datalist = new List<UserDetails>();
                        ViewBag.NoDevices = true;
                    }
                }
                else
                {
                    datalist = new List<UserDetails>();
                    ViewBag.NoDevices = true;
                }
                return View();
            }
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
            catch (Exception) { }
            return View(localDatalist);
        }
        [DynamicPermission("ComputerSummary.RemoteAccess", "Execute Remote Command")]
        public async Task<IActionResult> Comanddata(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (HttpClient client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/Command/" + domain + "");

                var content = new StringContent($"\"{"Scan"}\"", Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }
            return Redirect("Deshboad");
        }

        public async Task<IActionResult> CheckScanResult(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/SendScanStatus?domain={domain}");
                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    data.SoftwareName = "Notepad++";
                    return Json(data);
                }
                return Json(new { status = "failed" });

            }
        }

        [DynamicPermission("ComputerSummary.RemoteAccess", "Remote Control")]
        public async Task<IActionResult> RemoteAccess(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (HttpClient client = new HttpClient(handler))
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/SendScanStatus?domain={domain}");
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    data.SoftwareName = "Notepad++";
                    return Json(data);
                }
                return Json(new { status = "failed" });
            }
        }
        public async Task<IActionResult> CheckAccessStatus(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (var httpClient = new HttpClient(handler))
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (var httpClient = new HttpClient(handler))
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };
            var Mousedata = new MouseResponse
            {
                X = x,
                Y = y,
                Time = DateTime.Now,
            };
            using var client = new HttpClient(handler);
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };
            using var client = new HttpClient(handler);
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };
            using var client = new HttpClient(handler);
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
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };
            using var client = new HttpClient(handler);
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



        private HttpClient CreateClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };
            return new HttpClient(handler);
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

        public async Task<IActionResult> PatchUpdate([FromBody] InstallRequest req, string domain)
        {
            if (string.IsNullOrEmpty(domain) || req == null || string.IsNullOrEmpty(req.SoftwareName))
            {
                return Json(new { status = "failed", message = "Invalid request data" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var patchUpdateRequest = new PatchUpdateRequest
            {
                SoftwareName = req.SoftwareName,
                DownloadUrl = $"{_baseUrl}/installers/{req.SoftwareName}"
            };

            using (HttpClient client = new HttpClient(handler))
            {

                client.BaseAddress = new Uri($"{_baseUrl}/api/Command/update/" + domain + "");

                string jsonData = JsonConvert.SerializeObject(patchUpdateRequest);
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

        [HttpPost]
        public IActionResult UploadSoftware(IFormFile file)
        {
            //if (file == null || file.Length == 0)
            //    return Json("No file selected");

            //string wwwPath = _hostEnvironment.WebRootPath;
            //string folderPath = Path.Combine(wwwPath, "Softwares");

            //if (!Directory.Exists(folderPath))
            //    Directory.CreateDirectory(folderPath);

            //string filePath = Path.Combine(folderPath, file.FileName);

            //using (var stream = new FileStream(filePath, FileMode.Create))
            //{
            //    file.CopyTo(stream);
            //}

            return Json("Software uploaded successfully");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return Json(new { status = "failed", message = "fileName missing" });

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, ssl) => true
            };

            using (HttpClient client = new HttpClient(handler))
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


        public async Task<IActionResult> Uninstallsoftware([FromBody] UninstallRequest request, string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var patchUpdateRequest = new PatchUpdateRequest
            {
                SoftwareName = request.SoftwareName,
                DownloadUrl = "https://chrome.com/latest-update.exe"
            };
            using (HttpClient client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"{_baseUrl}/api/Command/softwareName/" + domain + "");
                //var content = new StringContent($"\"{"Update"}\"", Encoding.UTF8, "application/json");
                //HttpResponseMessage response = await client.PostAsync("", content);
                //string result = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
                string jsonData = JsonConvert.SerializeObject(patchUpdateRequest);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                string result = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { status = "success", message = "Uninstall command sent" });
                }
                else
                {
                    return Json(new { status = "failed", message = result });
                }
            }

        }
        public async Task<IActionResult> Uninstallsoftwarestatus(string softwareName, string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/uninstallstatus?softwareName={softwareName}&domain={domain}");
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    data.SoftwareName = "Notepad++";
                    return Json(data);
                }
                return Json(new { status = "failed" });
            }
        }
        public async Task<IActionResult> installsoftwarestatus(string softwareName, string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/installstatus?softwareName={softwareName}&domain={domain}");
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);
                    data.SoftwareName = "Notepad++";
                    return Json(data);
                }
                return Json(new { status = "failed" });
            }
        }

        public async Task<IActionResult> SoftwareInstaller()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<InstallerInfo>();
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Command/installers/");
                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    datalist = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstallerInfo>>(content) : null;
                    return View(datalist);
                }
                return View(datalist);
            }
        }




        [HttpGet]
        public async Task<IActionResult> users(string domain)
        {
            var localDatalist = new List<WindowsUserDetails>();
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/WindowsUserDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    localDatalist = data?.Where(x => x.DomainName == domain).ToList() ?? new List<WindowsUserDetails>();
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
            var localDatalist = new List<WindowDrivers>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/WindowDrivers");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<WindowDrivers>>(content);
                    localDatalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<WindowDrivers>();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
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
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }
            }
            catch (Exception) { }
            return Json(localDatalist);
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
            var localDatalist = new List<ProcessorDetails>();
            try
            {
                string UCode = GetUCodeFromDomain(domain);
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/ProcessorDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<ProcessorDetails>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { ProcessorSpeed = "N/A", Manufacturer = "N/A", Stepping = "N/A", Family = "N/A", NumberOfCores = 0, SocketDesignation = "N/A", Voltage = "N/A", Version = "N/A", DeviceStatus = "N/A", Description = "N/A", DateTime = DateTime.Now });
                }

                var processerdata = new
                {
                    ProcessorSpeed = localDatalist[0].ProcessorSpeed,
                    Manufacturer = localDatalist[0].Manufacturer,
                    Stepping = localDatalist[0].Stepping,
                    Family = localDatalist[0].Family,
                    NumberOfCores = localDatalist[0].NumberOfCores,
                    SocketDesignation = localDatalist[0].SocketDesignation,
                    Voltage = localDatalist[0].Voltage,
                    Version = localDatalist[0].Version,
                    DeviceStatus = localDatalist[0].DeviceStatus,
                    Description = localDatalist[0].Description,
                    DateTime = localDatalist[0].DateTime
                };

                return Json(processerdata);
            }
            catch (Exception)
            {
                return Json(new { ProcessorSpeed = "N/A", Manufacturer = "N/A", Stepping = "N/A", Family = "N/A", NumberOfCores = 0, SocketDesignation = "N/A", Voltage = "N/A", Version = "N/A", DeviceStatus = "N/A", Description = "N/A", DateTime = DateTime.Now });
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
                return BadRequest($"Error uninstalling software: {ex.Message}");
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

                var response = await httpClient.GetAsync("");
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

                var response = await httpClient.GetAsync("");
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
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceRestrictionDetails>>(content) : null;
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { IsCameraEnabled = "N/A", IsTelemetryEnabled = "N/A", CanModifyDateTime = "N/A", IsBluetoothEnabled = "N/A" });
                }

                var Restricationdeetailsfist = new
                {
                    IsCameraEnabled = localDatalist[0].IsCameraEnabled,
                    IsTelemetryEnabled = localDatalist[0].IsTelemetryEnabled,
                    CanModifyDateTime = localDatalist[0].IsCameraEnabled,
                    IsBluetoothEnabled = localDatalist[0].IsBluetoothEnabled
                };
                return Json(Restricationdeetailsfist);
            }
            catch (Exception)
            {
                return Json(new { IsCameraEnabled = "N/A", IsTelemetryEnabled = "N/A", CanModifyDateTime = "N/A", IsBluetoothEnabled = "N/A" });
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

        //BatteryInfo
        public async Task<IActionResult> Battery(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();
            var localDatalist = new List<BatteryInfo>();
            string UCode = GetUCodeFromDomain(domain);
            try
            {
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/Battery");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<BatteryInfo>>(content);
                    if (data != null) localDatalist = data.Where(x => x.UserCode == UCode).ToList();
                }

                if (!localDatalist.Any())
                {
                    return Json(new { Manufacturer = "N/A", Status = "N/A", Description = "N/A", BatteryLevel = 0, SystemType = "N/A", UserCode = UCode, DateTime = DateTime.Now });
                }

                var batterydata = new
                {
                    Manufacturer = localDatalist[0].Manufacturer,
                    Status = localDatalist[0].Manufacturer,
                    Description = localDatalist[0].Description,
                    BatteryLevel = localDatalist[0].BatteryLevel,
                    SystemType = localDatalist[0].SystemType,
                    UserCode = localDatalist[0].UserCode,
                    DateTime = localDatalist[0].DateTime
                };

                return Json(batterydata);
            }
            catch (Exception)
            {
                return Json(new { Manufacturer = "N/A", Status = "N/A", Description = "N/A", BatteryLevel = 0, SystemType = "N/A", UserCode = UCode, DateTime = DateTime.Now });
            }
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
                    var userDetail = data?.FirstOrDefault(x => x.domainName == domain);

                    if (userDetail != null)
                    {
                        return Json(new { success = true, ipAddress = userDetail.IpAddress ?? "N/A" });
                    }
                }
                return Json(new { success = false, message = "No data found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
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
                return Json(new { success = false, error = ex.Message });
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
                return Json(new { success = false, error = ex.Message });
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

        private string GetUCodeFromDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return "";
            var parts = domain.Split('-');
            return parts.Length > 1 ? parts[1] : domain;
        }
    }
}
