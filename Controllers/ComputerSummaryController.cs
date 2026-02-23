using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Dtos;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.UpdatesModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;

namespace ManageEngineWebApp.Controllers
{

    [AuthFilter]
    public class ComputerSummaryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ComputerSummaryController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<IActionResult> Deshboad(int locationId, string locationName, int groupid, string groupName, int comId, string companyName)
        {
            ViewBag.CompanyName = companyName;
            ViewBag.groupName = groupName;
            ViewBag.locationName = locationName;
            ViewBag.companyid = comId;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationId;

            var dalalist = new List<WindowsUserDetails>();
            var contectlist = new List<ConnectedClientDto>();
            List<string> activeComputers = new List<string>();

            // Use the named client "ManageEngineApi" configured in Program.cs
            var httpClient = _httpClientFactory.CreateClient("ManageEngineApi");

            try
            {
                // 1. Fetch WindowsUserDetails
                // Use relative path since BaseAddress is set in Program.cs/CreateClient
                string userUrl = $"api/WindowsUserDetails/allUser?locationId={locationId}&groupid={groupid}&comId={comId}";
                var response = await httpClient.GetAsync(userUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    if (data != null)
                    {
                        dalalist = data.Where(x => x != null && x.Status == "Enabled").ToList();
                    }
                }

                // 2. Fetch Connected Devices
                var response2 = await httpClient.GetAsync("api/Command/GetConnectedDevices");

                if (response2.IsSuccessStatusCode)
                {
                    var content2 = await response2.Content.ReadAsStringAsync();
                    contectlist = !string.IsNullOrEmpty(content2) ? JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content2) : null;

                    if (contectlist != null)
                    {
                        activeComputers = contectlist.Where(d => d != null).Select(d => d.UserName ?? "Unknown").ToList();
                    }
                }
            }
            catch (Exception)
            {
                 // Proceed with empty lists to avoid crashing the view
            }

            if (contectlist != null)
            {
                ViewBag.ActiveComputers = activeComputers;
            }

            return View(dalalist);
        }
        [AuthFilter]
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
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ManageEngineApi");
                string url = $"api/WindowsUserDetails/allUser?locationId={locationId}&groupid={groupId}&comId={companyId}";
                
                var response = await httpClient.GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content)
                        ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content)
                        : null;
                    var deviceList = (data ?? new List<WindowsUserDetails>())
                        .Where(x => x != null && x.Status == "Enabled")
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
            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var response = await client.GetAsync($"api/WindowsUserDetails/allUserByCompany?comId={companyId}");
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
            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var response = await client.GetAsync($"api/WindowsUserDetails/allUser?locationId={locationId}&groupid={groupId}&comId={companyId}");
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
            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                // Use relative path
                var response = await client.GetAsync("api/RamCpuDiskData/list");

                if (response != null && response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var allCriticalClients = JsonConvert.DeserializeObject<List<VIPClient>>(content);
                    // Note: This calls the internal method, not an API endpoint
                    var devicesResponse = await GetAllDevices(companyId, groupId ?? 0, locationId ?? 0);
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
            if (criticalClient == null ||
                string.IsNullOrEmpty(criticalClient.ClientId) ||
                string.IsNullOrEmpty(criticalClient.ClientName))
            {
                return Json(new { success = false, message = "Invalid client data" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ManageEngineApi");
                var dto = new
                {
                    ClientId = criticalClient.ClientId,
                    ClientName = criticalClient.ClientName
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
            {
                return Json(new { success = false, message = "Invalid client ID" });
            }

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
            if (string.IsNullOrEmpty(domain))
            {
                return Json(new { status = "error", error = "Domain is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(25);
                    string url = $"https://localhost:7225/api/RamCpuDiskData/{Uri.EscapeDataString(domain)}";

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetRamCpuDiskData Error: {ex.Message}");
                return Json(new { status = "error", error = ex.Message });
            }
        }


        //[HttpGet]
        //public async Task<IActionResult> GetRamCpuDiskData(string domain)
        //{
        //    if (string.IsNullOrEmpty(domain))
        //    {
        //        return Json(new { success = false, error = "Domain is required" });
        //    }

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    try
        //    {
        //        using (HttpClient client = new HttpClient(handler))
        //        {
        //            client.Timeout = TimeSpan.FromSeconds(25);
        //            string url = $"https://localhost:7225/api/RamCpuDiskData/{Uri.EscapeDataString(domain)}";

        //            HttpResponseMessage response = await client.GetAsync(url);

        //            if (response != null && response.IsSuccessStatusCode)
        //            {
        //                var content = await response.Content.ReadAsStringAsync();
        //                var apiResponse = JsonConvert.DeserializeObject<dynamic>(content);

        //                if (apiResponse.status == "success" && apiResponse.data != null)
        //                {
        //                    string dataString = apiResponse.data.ToString();
        //                    var usageData = ParseUsageString(dataString);

        //                    return Json(new { status = "success", data = usageData });
        //                }

        //                return Json(new { status = "error", error = "Invalid response format" });
        //            }
        //            else if (response != null && response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        //            {
        //                return Json(new { status = "timeout", error = "Client did not respond" });
        //            }

        //            return Json(new { status = "error", error = "Failed to fetch data" });
        //        }
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return Json(new { status = "timeout", error = "Request timeout" });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"GetRamCpuDiskData Error: {ex.Message}");
        //        return Json(new { status = "error", error = ex.Message });
        //    }
        //}

        //private object ParseUsageString(string dataString)
        //{
        //    try
        //    {
        //        var result = new Dictionary<string, double>();

        //        var cleanedString = dataString.Replace("%", "").Trim();
        //        var parts = cleanedString.Split(new[] { '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        //        foreach (var part in parts)
        //        {
        //            var trimmedPart = part.Trim();

        //            if (trimmedPart.ToUpper().StartsWith("CPU="))
        //            {
        //                var value = trimmedPart.Split('=')[1].Trim();
        //                if (double.TryParse(value, out double cpuVal))
        //                {
        //                    result["cpuUsage"] = cpuVal;
        //                }
        //            }
        //            else if (trimmedPart.ToUpper().StartsWith("RAM="))
        //            {
        //                var value = trimmedPart.Split('=')[1].Trim();
        //                if (double.TryParse(value, out double ramVal))
        //                {
        //                    result["ramUsage"] = ramVal;
        //                }
        //            }
        //            else if (trimmedPart.ToUpper().StartsWith("DISK="))
        //            {
        //                var value = trimmedPart.Split('=')[1].Trim();
        //                if (double.TryParse(value, out double diskVal))
        //                {
        //                    result["diskUsage"] = diskVal;
        //                }
        //            }
        //        }



        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error parsing usage string: {ex.Message}");
        //        return new { cpuUsage = 0, ramUsage = 0, diskUsage = 0 };
        //    }
        //}


        [HttpGet]
        public async Task<IActionResult> GetLastSeenTime(string domain)
        {
            if (string.IsNullOrEmpty(domain))
            {
                return Json(new { success = false, error = "Domain is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    string url = "https://localhost:7225/api/Client";

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetLastSeenTime Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EnableTempProtection(string machineId)
        {
            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string url = $"https://localhost:7225/api/OtpVerification/OtpCode?Massage=ON&machineId={Uri.EscapeDataString(machineId)}";

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
                                var otpResponse = await client.GetAsync($"https://localhost:7225/api/OtpVerification/get-otp?machineId={Uri.EscapeDataString(machineId)}");
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
            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    string url = $"https://localhost:7225/api/OtpVerification/get-otp?machineId={Uri.EscapeDataString(machineId)}";

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
            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    string otpUrl = $"https://localhost:7225/api/OtpVerification/OtpCode?Massage=ON&machineId={Uri.EscapeDataString(machineId)}";
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTempProtectionStatus Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GenerateOtpCode(string machineId)
        {
            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    string url = $"https://localhost:7225/api/OtpVerification/OtpGanrate?Massage=GENERATE&machineId={Uri.EscapeDataString(machineId)}";

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
            if (string.IsNullOrEmpty(machineId))
            {
                return Json(new { success = false, error = "MachineId is required" });
            }

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            try
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string url = $"https://localhost:7225/api/OtpVerification/OtpCode?Massage=OFF&machineId={Uri.EscapeDataString(machineId)}";

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClearOtpCode Error: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }


        public async Task<IActionResult> BranchPatchMangnment(int companyid, int groupid, int locationid)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;


            var dalalist = new List<WindowsUserDetails>();
            var contectlist = new List<ConnectedClientDto>();
            List<string> activeComputers = new List<string>();
            using (var httpClient = new HttpClient(handler))
            {




                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/WindowsUserDetails/allUser?locationId={locationid}&&groupid={groupid}&&comId={companyid}");
                //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/WindowsUserDetails/allUser?locationId={locationId}&&groupid={groupid}&&comId={comId}");
                //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/WindowsUserDetails/allUser?locationId={locationId}&&groupid={groupid}&&comId={comId}"); // Replace with your server's URL

                //var jsonContent = JsonConvert.SerializeObject(systemInfometion);
                //var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send POST request to the server
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    // var datalist =  JsonSerializer.Deserialize<List<WindowsUserDetails>>(content);
                    dalalist = data.Where(x => x.Status == "Enabled").ToList();
                    //return View(dalalist);
                }

                //var response2 = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices"); // Replace with actual path
                //var response2 = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices"); // Replace with actual path
                var response2 = await httpClient.GetAsync("https://localhost:7225/api/Command/GetConnectedDevices"); // Replace with actual path



                if (response2.IsSuccessStatusCode)
                {
                    var content2 = await response2.Content.ReadAsStringAsync();
                    contectlist = !string.IsNullOrEmpty(content2) ? JsonConvert.DeserializeObject<List<ConnectedClientDto>>(content2) : null;

                    activeComputers = contectlist.Select(d => d.UserName).ToList();
                }



                //return View(dalalist);

            }
            if (contectlist != null)
            {
                ViewBag.ActiveComputers = activeComputers;
            }


            return View(dalalist);

            throw new Exception("Unable to fetch data from the API.");
        }
        public async Task<IActionResult> BranchPatchselection(int companyid, int groupid, int locationid, string selectedIds)
        {
            // Convert selectedIds to list
            var idList = selectedIds.Split(',').Select(Int32.Parse).ToList();

            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.selectedIds = selectedIds;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            HttpClientHandler handler1 = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<PatchDetailsservice>();
            var repoList = new List<SoftwareRepoDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetailsservice>>(content) : null;
                    datalist = (data ?? new List<PatchDetailsservice>()).Where(x => x != null && x.UserCode != null).ToList();
                    //return View(datalist);
                }



                // ---------- API 2 : Software Repo ----------


                //return View(datalist);

            }

            using (var httpClient2 = new HttpClient(handler1))
            {
                // httpClient2.BaseAddress = new Uri("https://localhost:7225/api/SoftwareRepoDetails");
                httpClient2.BaseAddress = new Uri("https://localhost:7225/api/SoftwareRepoDetails");

                var response1 = await httpClient2.GetAsync("");
                //var response1 = await httpClient2.GetAsync("https://localhost:7225/api/SoftwareRepoDetails");
                if (response1.IsSuccessStatusCode)
                {
                    var content1 = await response1.Content.ReadAsStringAsync();
                    repoList = JsonConvert.DeserializeObject<List<SoftwareRepoDetails>>(content1);
                }
            }
            // ----- Compare Logic -----
            // Match Patch name with software repo name
            foreach (var item in datalist)
            {
                item.IsAvailableInRepo = repoList.Any(s => s.Version == item.AvailableVersion);
            }

            return View(datalist);
        }
        public async Task<IActionResult> BranchWinPatchselection(int companyid, int groupid, int locationid, string selectedIds)
        {
            // Convert selectedIds to list
            var idList = selectedIds.Split(',').Select(Int32.Parse).ToList();

            ViewBag.companyid = companyid;
            ViewBag.groupid = groupid;
            ViewBag.locationid = locationid;
            ViewBag.selectedIds = selectedIds;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            HttpClientHandler handler1 = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<PatchDetail>();
            var repoList = new List<SoftwareRepoDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //  httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch/windowpatch");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch/windowpatch");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetail>>(content) : null;
                    datalist = data.Where(x => x.UserCode != null).ToList();
                    return View(datalist);
                }



                // ---------- API 2 : Software Repo ----------


                //return View(datalist);

            }

            //using (var httpClient2 = new HttpClient(handler1))
            //{
            //    httpClient2.BaseAddress = new Uri("https://localhost:7225/api/SoftwareRepoDetails");

            //    var response1 = await httpClient2.GetAsync("");
            //    //var response1 = await httpClient2.GetAsync("https://localhost:7225/api/SoftwareRepoDetails");
            //    if (response1.IsSuccessStatusCode)
            //    {
            //        var content1 = await response1.Content.ReadAsStringAsync();
            //        repoList = JsonConvert.DeserializeObject<List<SoftwareRepoDetails>>(content1);
            //    }
            //}
            //// ----- Compare Logic -----
            //// Match Patch name with software repo name
            //foreach (var item in datalist)
            //{
            //    item.IsAvailableInRepo = repoList.Any(s => s.Version == item.AvailableVersion);
            //}

            return View(datalist);
        }

        public async Task<IActionResult> UpdatePatchselection(int companyid, int groupid, int locationid, string selectedIds, string domainids)
        {
            var idList = selectedIds.Split(',').Select(int.Parse).ToList();



            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            UpdatePatchselectiondto updatePatchselectiondto = new UpdatePatchselectiondto();
            updatePatchselectiondto.companyid = companyid;
            updatePatchselectiondto.groupid = groupid;
            updatePatchselectiondto.locationid = locationid;
            updatePatchselectiondto.selectedIds = selectedIds;
            updatePatchselectiondto.domainids = domainids;

            using (HttpClient client = new HttpClient(handler))
            {

                // client.BaseAddress = new Uri("https://localhost:7225/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchsethirdparty");
                client.BaseAddress = new Uri("https://localhost:7225/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchsethirdparty");

                string jsonData = JsonConvert.SerializeObject(updatePatchselectiondto);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                //return Json(result);
                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }

            return View();
        }

        //UpdatewinPatchselection

        public async Task<IActionResult> UpdatewinPatchselection(int companyid, int groupid, int locationid, string selectedIds, string domainids)
        {

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            UpdatewinPatchselectiondto updatewinPatchselectiondto = new UpdatewinPatchselectiondto();
            updatewinPatchselectiondto.companyid = companyid;
            updatewinPatchselectiondto.groupid = groupid;
            updatewinPatchselectiondto.locationid = locationid;
            updatewinPatchselectiondto.selectedIds = selectedIds;
            updatewinPatchselectiondto.domainids = domainids;

            using (HttpClient client = new HttpClient(handler))
            {

                // client.BaseAddress = new Uri("https://localhost:7225/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchwindowpatch");
                client.BaseAddress = new Uri("https://localhost:7225/api/MultipleWindowThirdPartyPatchUpdate/UpdatePatchwindowpatch");

                string jsonData = JsonConvert.SerializeObject(updatewinPatchselectiondto);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                //return Json(result);
                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> deshboardupdate([FromBody] WindowsUserDetailsUpdateName windowsUserDetailsupdateName)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            if (windowsUserDetailsupdateName.FullName == null)
            {
                windowsUserDetailsupdateName.FullName = "";
            }
            if (windowsUserDetailsupdateName.UserName == null)
            {
                windowsUserDetailsupdateName.UserName = "";
            }


            using (HttpClient client = new HttpClient(handler))
            {

                client.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails/dashboardupdate");

                string jsonData = JsonConvert.SerializeObject(windowsUserDetailsupdateName);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");



                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<object>(result);
                return Json(jsonResponse);
                //return Json(result);
                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }



        }





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




                //  httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserDetails");
                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserDetails");




                // Send POST request to the server
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
        //            httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserDetails");

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

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<WindowsUserDetails>();
            using (var httpClient = new HttpClient(handler))
            {                                                                      


                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/Client");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/Client");
                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/Client");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");

                //var requestData = new { DomainName = domain }; // Include domain variable
                //  var jsonContent = new StringContent(JsonConvert.SerializeObject(), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ClientConnection>>(content) : null;

                    return View(data); // Return the fetched data
                }

                return View();

            }

        }
        public async Task<IActionResult> Comanddata(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (HttpClient client = new HttpClient(handler))
            {

                // client.BaseAddress = new Uri("https://localhost:7225/api/Command/" + domain + "");

                client.BaseAddress = new Uri("https://localhost:7225/api/Command/" + domain + "");

                var content = new StringContent($"\"{"Scan"}\"", Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }
            return Redirect("Deshboad");

        }







        public async Task<IActionResult> CheckScanResult(string domain)
        {
            //domain = "DESKTOP-T33QOLJ";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {


                // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/SendScanStatus?domain={domain}");
                //
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/SendScanStatus?domain={domain}");
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/Command/SendScanStatus/" + domain + "");



                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);

                    //var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MSGRequest>>(content) : null;
                    data.SoftwareName = "Notepad++";
                    return Json(data); // Return the fetched data
                }

                return Json(new { status = "failed" }); // Return error object

            }



        }


        public async Task<IActionResult> RemoteAccess(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (HttpClient client = new HttpClient(handler))
            {

                // client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/" + domain + "");

                client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/" + domain + "");

                var content = new StringContent($"\"{"Remote"}\"", Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("", content);

                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }
            return Redirect("Deshboad");

        }



        public async Task<IActionResult> Remotestatus(string domain)
        {
            //domain = "DESKTOP-T33QOLJ";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {


                // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/SendScanStatus?domain={domain}");

                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/SendScanStatus?domain={domain}");




                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);

                    //var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MSGRequest>>(content) : null;
                    data.SoftwareName = "Notepad++";
                    return Json(data); // Return the fetched data
                }

                return Json(new { status = "failed" }); // Return error object

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
                string url = $"https://localhost:7225/api/RemoteAccess/CheckStatus?domain={domain}";

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
                // string url = $"https://localhost:7225/api/RemoteAccess/monitor?domain={domain}";
                string url = $"https://localhost:7225/api/RemoteAccess/monitor?domain={domain}";

                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Client not found or no image received.";
                    return View();
                }

                var content = await response.Content.ReadAsStringAsync();

                // Convert to object
                var data = JsonConvert.DeserializeObject<monitordata>(content);

                return View(data);  // Pass data to View
            }
        }


        // mouse controll pannel Method
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


                //  client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/MouseEvent/" + domain + "");
                client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/MouseEvent/" + domain + "");

                string jsonData = JsonConvert.SerializeObject(Mousedata);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // ?? **POST Request Send Karein**
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

        // ?? Send Left Click
        public async Task<IActionResult> SendLeftClick(string domain)
        {


            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };



            using var client = new HttpClient(handler);
            {


                // client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/MouseLeftClick/" + domain + "");
                client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/MouseLeftClick/" + domain + "");

                string jsonData = JsonConvert.SerializeObject("");
                //var content = new StringContent($"\"{"Scan"}\"", Encoding.UTF8, "application/json");
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // ?? **POST Request Send Karein**
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


        // ?? Send Right Click
        public async Task<IActionResult> SendRightClick(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };



            using var client = new HttpClient(handler);
            {


                //client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/MouseRightClick/" + domain + "");
                client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/MouseRightClick/" + domain + "");

                string jsonData = JsonConvert.SerializeObject("");
                //var content = new StringContent($"\"{"Scan"}\"", Encoding.UTF8, "application/json");
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // ?? **POST Request Send Karein**
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

        // ?? Send Key Press
        public async Task<IActionResult> SendKeyPress(string domain, string key)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };



            using var client = new HttpClient(handler);
            {


                // client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/KeyEvent/" + domain + "");
                client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/KeyEvent?domain="+domain+"&key="+key+"");

                string jsonData = JsonConvert.SerializeObject("");
                var content = new StringContent($"\"{key}\"", Encoding.UTF8, "application/json");
                //var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // ?? **POST Request Send Karein**
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



        // Reusable HttpClient creator
        private HttpClient CreateClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, ssl) => true
            };

            return new HttpClient(handler);
        }


        public async Task<IActionResult> Livestop(string domain)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (HttpClient client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://localhost:7225/api/RemoteAccess/StopRemopte/" + domain + "");
                var content = new StringContent($"\"{"StopRemote"}\"", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("", content);
                return Json(new { success = response.IsSuccessStatusCode });
            }
        }
        public async Task<IActionResult> Remotemonitoring(string domain)
        {
            ViewBag.Domain = domain;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (var httpClient = new HttpClient(handler))
            {
                //string url = $"https://localhost:7225/api/RemoteAccess/monitor?domain={domain}";
                string url = $"https://localhost:7225/api/RemoteAccess/monitor?domain={domain}";

                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Client not found or no image received.";
                    return View();
                }

                var content = await response.Content.ReadAsStringAsync();

                // Convert to object
                var data = JsonConvert.DeserializeObject<monitordata>(content);

                return Json(data);  // Pass data to View
            }
        }







        public async Task<IActionResult> PatchUpdate([FromBody] InstallRequest req, string domain)
        {
            //domain ="DESKTOP-T33QOLJ";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var patchUpdateRequest = new PatchUpdateRequest
            {
                //SoftwareName = "npp.8.7.8.Installer.x64.exe",
                SoftwareName = req.SoftwareName,
                DownloadUrl = "https://chrome.com/latest-update.exe"
            };

            using (HttpClient client = new HttpClient(handler))
            {

                //client.BaseAddress = new Uri("https://localhost:7225/api/Command/update/" + domain + "");
                client.BaseAddress = new Uri("https://localhost:7225/api/Command/update/" + domain + "");

                string jsonData = JsonConvert.SerializeObject(patchUpdateRequest);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // ?? **POST Request Send Karein**
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

                // Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }
            // return Redirect("Deshboad");

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
                //string apiUrl = $"https://localhost:7225/api/PatchDetails/DeleteSoftware/{fileName}";
                string apiUrl = $"https://localhost:7225/api/PatchDetails/DeleteSoftware/{fileName}";

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
            //domain = "DESKTOP-T33QOLJ";
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

                //client.BaseAddress = new Uri("https://localhost:7225/api/Command/softwareName/" + domain + "");
                client.BaseAddress = new Uri("https://localhost:7225/api/Command/softwareName/" + domain + "");

                //var content = new StringContent($"\"{"Update"}\"", Encoding.UTF8, "application/json");

                //HttpResponseMessage response = await client.PostAsync("", content);

                //string result = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");

                // ?? **Model ko JSON String me Convert karein**
                string jsonData = JsonConvert.SerializeObject(patchUpdateRequest);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // ?? **POST Request Send Karein**
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
                //Console.WriteLine(response.IsSuccessStatusCode ? $"? Success: {result}" : $"? Error: {result}");
            }
            // return Redirect("Deshboad");

        }
        public async Task<IActionResult> Uninstallsoftwarestatus(string softwareName, string domain)
        {
            //domain = "DESKTOP-T33QOLJ";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {


                //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/uninstallstatus?softwareName={softwareName}&domain={domain}");
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/uninstallstatus?softwareName={softwareName}&domain={domain}");
                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/Command/uninstallstatus/" + domain + "");


                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);

                    //var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MSGRequest>>(content) : null;
                    data.SoftwareName = "Notepad++";
                    return Json(data); // Return the fetched data
                }

                return Json(new { status = "failed" }); // Return error object

            }



        }
        public async Task<IActionResult> installsoftwarestatus(string softwareName, string domain)
        {
            //domain = "DESKTOP-T33QOLJ";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<MSGRequest>();
            using (var httpClient = new HttpClient(handler))
            {


                // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/installstatus?softwareName={softwareName}&domain={domain}");
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/installstatus?softwareName={softwareName}&domain={domain}");
                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/Command/installstatus/" + domain + "");


                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<MSGRequest>(content);

                    //var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MSGRequest>>(content) : null;
                    data.SoftwareName = "Notepad++";
                    return Json(data); // Return the fetched data
                }

                return Json(new { status = "failed" }); // Return error object

            }



        }

        public async Task<IActionResult> SoftwareInstaller()
        {
            //domain = "DESKTOP-T33QOLJ";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<InstallerInfo>();
            using (var httpClient = new HttpClient(handler))
            {


                // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/installers/");
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/Command/installers/");

                //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");

                //var requestData = new { DomainName = domain }; // Include domain variable
                //  var jsonContent = new StringContent(JsonConvert.SerializeObject(), System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();


                    datalist = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstallerInfo>>(content) : null;

                    return View(datalist); // Return the fetched data
                }

                return View(datalist);

            }



        }




        [HttpGet]
        public async Task<IActionResult> users(string domain)
        {



            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<WindowsUserDetails>();

            using (var httpClient = new HttpClient(handler))
            {




                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    datalist = data.Where(x => x.DomainName == domain).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }


        }
        public async Task<IActionResult> Summary(string domain)
        {
            string UCode = GetUCodeFromDomain(domain);



            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<Summary>();





            using (var httpClient = new HttpClient(handler))
            {




                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/Summary");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/Summary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Summary>>(content) : null;
                    datalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<Summary>();
                    //return Json(datalist);
                }

                if (!datalist.Any())
                {
                    return Json(new { TotalHardware = 0, TotalSoftware = 0, CommercialSoftware = 0, NonCommercialSoftware = 0, ProhibitedSoftware = 0, MissingPatches = 0 });
                }


                var assetSummary = new
                {
                    TotalHardware = datalist[0].TotalHardware,
                    TotalSoftware = datalist[0].TotalSoftware,
                    CommercialSoftware = datalist[0].CommercialSoftware,
                    NonCommercialSoftware = datalist[0].NonCommercialSoftware,
                    ProhibitedSoftware = datalist[0].ProhibitedSoftware,
                    MissingPatches = datalist[0].MissingPatches
                };


                return Json(assetSummary);

            }


        }

        // OSSummary
        public async Task<IActionResult> OSSummary(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<OSSummary>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/OSSummary");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/OSSummary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummary>>(content) : null;
                    datalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<OSSummary>();
                    //return Json(datalist);
                }

                if (!datalist.Any())
                {
                    return Json(new { OperatingSystem = "N/A", OSVersion = "N/A", RegisteredTo = "N/A", ProductID = "N/A", LicenseType = "N/A", SystemDrive = "N/A", OSCDKey = "N/A", OSServicePack = "N/A", OSBuildNumber = "N/A" });
                }
                //return Json(datalist);

                var sosummary = new
                {
                    OperatingSystem = datalist[0].OperatingSystem,
                    OSVersion = datalist[0].OSVersion,
                    RegisteredTo = datalist[0].RegisteredTo,
                    ProductID = datalist[0].ProductID,
                    LicenseType = datalist[0].LicenseType,
                    SystemDrive = datalist[0].SystemDrive,
                    OSCDKey = datalist[0].OSCDKey,
                    OSServicePack = datalist[0].OSServicePack,
                    OSBuildNumber = datalist[0].OSBuildNumber,
                };
                return Json(sosummary);

            }
        }

        // DeviceSummary
        public async Task<IActionResult> DeviceSummary(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<DeviceSummary>();

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/DeviceSummary");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/DeviceSummary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceSummary>>(content) : null;
                    datalist = data?.Where(x => x.UserCode == UCode).ToList() ?? new List<DeviceSummary>();
                    //return Json(datalist);
                }

                if (!datalist.Any())
                {
                    return Json(new { DeviceName = "N/A", Manufacturer = "N/A", Model = "N/A", SystemType = "N/A", SerialNumber = "N/A", Domain = "N/A", UserName = "N/A", TimeZone = "N/A", TotalPhysicalMemory = "N/A" });
                }
                //return Json(datalist);
                var assetSummary = new
                {
                    DeviceManufacturer = datalist[0].DeviceManufacturer,
                    DeviceModel = datalist[0].DeviceModel,
                    DeviceType = datalist[0].DeviceType,
                    Processor = datalist[0].Processor,
                    Memory = datalist[0].Memory,
                    SerialNumber = datalist[0].SerialNumber,
                    ProcessorArchitecture = datalist[0].ProcessorArchitecture,
                    AssetTag = datalist[0].AssetTag,
                    UDID = datalist[0].UDID,
                    EASDeviceIdentifier = datalist[0].EASDeviceIdentifier,
                    BatteryLevel = datalist[0].BatteryLevel
                };


                return Json(assetSummary);
            }
        }





        [HttpGet]
        public async Task<IActionResult> UsegeDisk(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<DiskUsage>();

            using (var httpClient = new HttpClient(handler))
            {


                httpClient.BaseAddress = new Uri("https://localhost:7225/api/DiskUsage");
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/DiskUsage");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DiskUsage>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }

                var drivedata = new
                {
                    Drive = datalist[0].Drive,
                    TotalSpaceGB = datalist[0].TotalSpaceGB,
                    UsedSpaceGB = datalist[0].UsedSpaceGB,
                    FreeSpaceGB = datalist[0].FreeSpaceGB

                };

                return Ok(drivedata);
                //return Json(datalist);
            }

            ////return Json(datalist);




        }

        [HttpGet]
        public async Task<IActionResult> services(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<WindowsService>();

            using (var httpClient = new HttpClient(handler))
            {


                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsService");
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsService");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsService>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }

                return Json(datalist);
            }

            //return Json(datalist);




        }

        //groups
        [HttpGet]
        public async Task<IActionResult> groups(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<WindowsGroupDetails>();


            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsGroupDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsGroupDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsGroupDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //drivers
        [HttpGet]
        public async Task<IActionResult> drivers(string domain)
        {
            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<WindowDrivers>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowDrivers");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowDrivers");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //BIOS
        [HttpGet]
        public async Task<IActionResult> BIOS(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<BIOSDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/BIOSDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/BIOSDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BIOSDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                var biosdetaildata = new
                {
                    Manufacturer = datalist[0].Manufacturer,
                    Version = datalist[0].Version,
                    SMBiosVersion = datalist[0].SMBiosVersion,
                    ReleaseDate = datalist[0].ReleaseDate,
                    YearOfInstallation = datalist[0].YearOfInstallation,
                    Status = datalist[0].Status,
                    Description = datalist[0].Description,
                    DateTime = datalist[0].DateTime

                };


                return Json(biosdetaildata);

            }




        }

        //HardDisk
        [HttpGet]
        public async Task<IActionResult> HardDisk(string domain)
        {


            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<HardDiskDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/HardDiskDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/HardDiskDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<HardDiskDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //HardDisk
        [HttpGet]
        public async Task<IActionResult> LocalDisk(string domain)
        {


            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<LogicalDiskDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/LogicalDiskDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/LogicalDiskDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<LogicalDiskDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        // KeyboardDetails

        [HttpGet]
        public async Task<IActionResult> Keyboard(string domain)

        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<KeyboardDetails>();

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/KeyboardDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/KeyboardDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<KeyboardDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        // MonitorInfo

        public async Task<IActionResult> Monitor(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<MonitorInfo>();

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/MonitorInfo");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MonitorInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MonitorInfo>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }

                var monitordata1 = new
                {
                    Manufacturer = datalist[0].Manufacturer,
                    MonitorType = datalist[0].MonitorType,
                    ScreenHeight = datalist[0].ScreenHeight,
                    ScreenWidth = datalist[0].ScreenWidth,
                    DeviceStatus = datalist[0].DeviceStatus,
                    Description = datalist[0].Description,
                    SerialNumber = datalist[0].SerialNumber,
                    InstalledWeek = datalist[0].InstalledWeek,
                    InstalledYear = datalist[0].InstalledYear,
                    MonitorSize = datalist[0].MonitorSize,
                    DateTime = datalist[0].DateTime

                };
                return Json(monitordata1);

            }
        }

        [HttpGet]
        public async Task<IActionResult> Motherboard(string domain)
        {
            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };



            var datalist = new List<MotherboardDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/MotherboardDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MotherboardDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MotherboardDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        [HttpGet]
        public async Task<IActionResult> NetworkAdapters(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<NetworkAdapterDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/NetworkAdapterDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/NetworkAdapterDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<NetworkAdapterDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                return Json(datalist);

            }
        }

        //PhysicalMemory
        [HttpGet]
        public async Task<IActionResult> PhysicalMemory(string domain)
        {


            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<PhysicalMemoryDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/PhysicalMemoryDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/PhysicalMemoryDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PhysicalMemoryDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }

                var physicalmemo = new
                {
                    MaximumSupportedRAM = datalist[0].MaximumSupportedRAM,
                    Location = datalist[0].Location,
                    SlotsAvailable = datalist[0].SlotsAvailable,
                    SlotsUsed = datalist[0].SlotsUsed,
                    // Slots = await MemorySlotDetails.list()
                };
                return Json(physicalmemo);

            }


        }


        //MemorySlotDetails
        [HttpGet]
        public async Task<IActionResult> MemorySlotDetails(string domain)
        {


            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<MemorySlotDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/MemorySlotDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MemorySlotDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MemorySlotDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }


                return Json(datalist);

            }


        }


        //PointingDeviceInfo


        public async Task<IActionResult> PointingDevices(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<PointingDeviceInfo>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/PointingDeviceInfo");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/PointingDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PointingDeviceInfo>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        //Printers

        [HttpGet]
        public async Task<IActionResult> Printers(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<PrinterDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/PrinterDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/PrinterDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PrinterDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //Processors
        [HttpGet]
        public async Task<IActionResult> Processors(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<ProcessorDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/ProcessorDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/ProcessorDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ProcessorDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                var processerdata = new
                {
                    ProcessorSpeed = datalist[0].ProcessorSpeed,
                    Manufacturer = datalist[0].Manufacturer,
                    Stepping = datalist[0].Stepping,
                    Family = datalist[0].Family,
                    NumberOfCores = datalist[0].NumberOfCores,
                    SocketDesignation = datalist[0].SocketDesignation,
                    Voltage = datalist[0].Voltage,
                    Version = datalist[0].Version,
                    DeviceStatus = datalist[0].DeviceStatus,
                    Description = datalist[0].Description,
                    DateTime = datalist[0].DateTime

                };

                return Json(processerdata);

            }

        }

        //Sound

        public async Task<IActionResult> Sound(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<SoundDeviceDetails>();
            using (var httpClient = new HttpClient(handler))
            {
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/SoundDeviceDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/SoundDeviceDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoundDeviceDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }



            throw new Exception("Unable to fetch data from the API.");
        }


        //VideoDeviceInfo

        public async Task<IActionResult> VideoControllers(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<VideoDeviceInfo>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/VideoDeviceInfo");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/VideoDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<VideoDeviceInfo>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //USBControllerInfo

        public async Task<IActionResult> USBControllers(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<USBControllerInfo>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/USBControllerInfo");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/USBControllerInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBControllerInfo>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //USBHub

        public async Task<IActionResult> USBHub(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<USBHubDetails>();
            using (var httpClient = new HttpClient(handler))
            {
                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/USBHubDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/USBHubDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBHubDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }


            throw new Exception("Unable to fetch data from the API.");
        }



        //DesktopApps
        [HttpGet]
        public async Task<IActionResult> DesktopApps(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<DesktopAppsModel>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication/DesktopApps");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication/DesktopApps");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DesktopAppsModel>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        [HttpPost]
        public IActionResult Uninstall([FromBody] UninstallRequest request, string domain)
        {
            try
            {
                string softwareName = request.SoftwareName;

                // PowerShell command to uninstall
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




        //MicrosoftstoreApps

        public async Task<IActionResult> MicrosoftstoreApps(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<MicrosoftStoreAppDetailsClass>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MicrosoftStoreAppDetailsClass>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }


        }
        //MeteredSoftware

        public async Task<IActionResult> MeteredSoftware(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var datalist = new List<InstalledApplication>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication/MeteredSoftware");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication/MeteredSoftware");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        //InstallationSoftware
        public async Task<IActionResult> InstallationSoft(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            var data = new List<SoftwareFileModel>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/InstalledApplication/InstallationSoftlist?domain={domain}");
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/InstalledApplication/InstallationSoftlist?domain={domain}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoftwareFileModel>>(content) : null;

                    //return Json(datalist);
                }
                return Json(data);

            }
        }

        //AntivirusDetails

        public async Task<IActionResult> Antivirus(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<AntivirusDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/AntivirusDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        //patch universe update
        public async Task<IActionResult> Missingpatch(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<PatchDetailsservice>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch");

                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetailsservice>>(content) : null;
                    if (data != null) datalist = data.Where(x => x != null && x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        //Missingpatchwindow


        public async Task<IActionResult> Missingpatchwindow(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<PatchDetail>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch/windowpatch");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MissingPatch/windowpatch");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetail>>(content) : null;
                    if (data != null) datalist = data.Where(x => x != null && x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }
        //Firewall
        public async Task<IActionResult> Firewall(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<AntivirusDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/AntivirusDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        // Missing Patch
        public async Task<IActionResult> MissingPatches(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<PatchDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/PatchDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/PatchDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PatchDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        [HttpPost]
        public IActionResult UpdatePatches([FromBody] List<string> patches, string domain)
        {
            foreach (var patchId in patches)
            {
                // Yahan aap remote client ko CMD/PowerShell trigger kar sakte ho
                // Example: call client agent API for that patch
            }
            return Ok(new { message = "Update commands sent" });
        }


        // RestrictionOnDevice

        public async Task<IActionResult> RestrictionOnDevice(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<DeviceRestrictionDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/DeviceRestrictionDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/DeviceRestrictionDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceRestrictionDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                var Restricationdeetailsfist = new
                {
                    IsCameraEnabled = datalist[0].IsCameraEnabled,
                    IsTelemetryEnabled = datalist[0].IsTelemetryEnabled,
                    CanModifyDateTime = datalist[0].IsCameraEnabled,
                    IsBluetoothEnabled = datalist[0].IsBluetoothEnabled


                };

                return Json(Restricationdeetailsfist);

            }
        }

        public async Task<IActionResult> RestrictionOnNetwork(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<RestrictionOnNetwork>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri($"https://localhost:7225/api/RestrictionOnDevice/RestrictiononNetwork?domain={domain}");
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/RestrictionOnDevice/RestrictiononNetwork?domain={domain}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<RestrictionOnNetwork>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                var RestricationNettailsfist = new
                {
                    InternetSharing = datalist[0].InternetSharing,
                    VPN = datalist[0].VPN,
                    WiFi = datalist[0].WiFi,
                    AllowWiFiConfiguration = datalist[0].AllowWiFiConfiguration,
                    AutoConnectWiFiSense = datalist[0].AutoConnectWiFiSense


                };

                return Json(RestricationNettailsfist);

            }
        }
        public async Task<IActionResult> bluetootdetailsdata(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<BluetoothDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri($"https://localhost:7225/api/RestrictionOnDevice/BluetoothDetails?domain={domain}");
                httpClient.BaseAddress = new Uri($"https://localhost:7225/api/RestrictionOnDevice/BluetoothDetails?domain={domain}");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BluetoothDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                var bluetootdetailslist = new
                {
                    Bluetooth = datalist[0].Bluetooth,
                    Bluetoothdiscovery = datalist[0].Bluetoothdiscovery,
                    Bluetoothprepairing = datalist[0].Bluetoothprepairing,
                    Bluetoothservicesadvertising = datalist[0].Bluetoothservicesadvertising

                };

                return Json(bluetootdetailslist);

            }
        }




        //SecurityPrivacyDetails

        public async Task<IActionResult> SecurityPrivacyDetails(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<SecurityPrivacyDetails>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/SecurityPrivacyDetails");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/SecurityPrivacyDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SecurityPrivacyDetails>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    // return Json(datalist);
                }

                var Security = new
                {
                    LocationServices = datalist[0].LocationServices,
                    IsMicrosoftAccountConnected = datalist[0].IsMicrosoftAccountConnected,
                    CanAddNonMicrosoftAccounts = datalist[0].CanAddNonMicrosoftAccounts,
                    CanResetDevice = datalist[0].CanResetDevice,

                };
                return Json(Security);

            }
        }


        //ApplicationSettings

        public async Task<IActionResult> ApplicationSettings(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<ApplicationSettings>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/ApplicationSettings");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/ApplicationSettings");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ApplicationSettings>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    // return Json(datalist);
                }

                var application = new
                {
                    InstallNonStoreApps = datalist[0].InstallNonStoreApps,
                    InstallAppsOnlyInDeviceMemory = datalist[0].InstallAppsOnlyInDeviceMemory,
                    StoreAppDataOnlyInDeviceMemory = datalist[0].StoreAppDataOnlyInDeviceMemory,
                    AutoUpdateStoreApps = datalist[0].AutoUpdateStoreApps
                };
                return Json(application);

            }
        }

        //SocialSearchSettings

        public async Task<IActionResult> SocialSearchSettings(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<SocialSearchSettings>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/SocialSearchSettings");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/SocialSearchSettings");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SocialSearchSettings>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    // return Json(datalist);
                }

                var social = new
                {
                    CortanaEnabled = datalist[0].CortanaEnabled,
                    SyncSettingsEnabled = datalist[0].SyncSettingsEnabled,
                    SearchLocationEnabled = datalist[0].SearchLocationEnabled
                };
                return Json(social);

            }
        }



        //UsbAudit

        public async Task<IActionResult> UsbDeviceAudit(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<USBDeviceInfo>();

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/UsbDeviceInfo");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UsbDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBDeviceInfo>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        //AuditHistory
        public async Task<IActionResult> AuditHistory(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var datalist = new List<UserAuditHistory>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserAuditHistory");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserAuditHistory");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserAuditHistory>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        //LoginHistory

        public async Task<IActionResult> LoginHistory(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<UserLogonHistory>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserLogonHistory");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserLogonHistory");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserLogonHistory>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        // Update Log user
        public async Task<IActionResult> UpdateLoguser(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<WindowsUserDetailsUpdates>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/UpdateLogs");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UpdateLogs");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetailsUpdates>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //BatteryInfo
        public async Task<IActionResult> Battery(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var datalist = new List<BatteryInfo>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/Battery");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/Battery");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BatteryInfo>>(content) : null;
                    if (data != null) datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(datalist);
                }
                var batterydata = new
                {
                    Manufacturer = datalist[0].Manufacturer,
                    Status = datalist[0].Manufacturer,
                    Description = datalist[0].Description,
                    BatteryLevel = datalist[0].BatteryLevel,
                    SystemType = datalist[0].SystemType,
                    UserCode = datalist[0].UserCode,
                    DateTime = datalist[0].DateTime

                };

                return Json(batterydata);

            }

            throw new Exception("Unable to fetch data from the API.");
        }



        //SummaryUpdateLog
        public async Task<IActionResult> SummaryUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<SummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Summarydata/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Summarydata/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //OSSummarydata
        public async Task<IActionResult> OSSummaryUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<OSSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/OSSummarydata/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/OSSummarydata/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }



        //DeviceSummaryChangeAuditUpdateLog
        public async Task<IActionResult> DeviceSummaryChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<DeviceSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/DeviceSummarylist/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/DeviceSummarylist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //BiosSummaryChageUpdateLog
        public async Task<IActionResult> BiosSummaryChageUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<BiosSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Bioslist/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Bioslist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BiosSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        //HardDiskSummaryChangeAuditUpdateLog
        public async Task<IActionResult> HardDiskSummaryChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<HardDiskSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Harddisklist/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Harddisklist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<HardDiskSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //KeyboardSummaryChangeAuditUpdateLog
        public async Task<IActionResult> KeyboardSummaryChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<KeyboardSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Keyboardlist/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Keyboardlist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<KeyboardSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //MonitorSummaryChangeAuditUpdateLog
        public async Task<IActionResult> MonitorSummaryChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<MonitorSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Monitorlist/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Monitorlist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MonitorSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    // return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //MotherboardSummaryChangeAuditUpdateLog
        public async Task<IActionResult> MotherboardSummaryChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<MotherboardSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/MotherboardSummaryChangeAudit/" + UCode +"");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/MotherboardSummaryChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MotherboardSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }




        //NetworkAdapterChangeAuditUpdateLog
        public async Task<IActionResult> NetworkAdapterChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<NetworkAdapterChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Networkadhapterlist/" + UCode +"");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/Networkadhapterlist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<NetworkAdapterChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //ProcessorChangeAuditUpdateLog
        public async Task<IActionResult> ProcessorChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<ProcessorChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/ProcessorChangeAuditlist/"+UCode+"");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/ProcessorChangeAuditlist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ProcessorChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        //physicalMemoryDetailsChangeAudit

        public async Task<IActionResult> physicalMemoryDetailsChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<physicalMemoryDetailsChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/physicalMemoryDetailsChangeAudit/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/physicalMemoryDetailsChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<physicalMemoryDetailsChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //SoundDeviceChangeAuditUpdateLog
        public async Task<IActionResult> SoundDeviceChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<SoundDeviceChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/SoundDeviceChangeAuditlist/" + UCode +"");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/SoundDeviceChangeAuditlist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoundDeviceChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //USBControllerChangeAuditUpdateLog
        public async Task<IActionResult> USBControllerChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<OSSummaryChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/USBControllerChangeAuditlist/" + UCode+"");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/USBControllerChangeAuditlist/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummaryChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //WindowsUserChangeAudit


        public async Task<IActionResult> WindowsUserChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<WindowsUserChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/WindowsUserChangeAudit/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/WindowsUserChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //WindowsGroupChangeAudit

        public async Task<IActionResult> WindowsGroupChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<WindowsGroupChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/WindowsGroupChangeAudit/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/WindowsGroupChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsGroupChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //WindowDriversChangeAudit
        public async Task<IActionResult> WindowDriversChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<WindowDriversChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/WindowDriversChangeAudit/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/WindowDriversChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDriversChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //DesktopAppsChangeAuditUpdateLog
        public async Task<IActionResult> DesktopAppsChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<DesktopAppsChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/DesktopAppsChangeAudit/ " + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/DesktopAppsChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DesktopAppsChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    //return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //MSStoreAppChangeAudit

        public async Task<IActionResult> MSStoreAppChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<MSStoreAppChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                // httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/MSStoreAppChangeAudit/"  + UCode +"");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/MSStoreAppChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MSStoreAppChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //AntivirusChangeAudit

        public async Task<IActionResult> AntivirusChangeAuditUpdateLog(string domain)
        {

            string UCode = GetUCodeFromDomain(domain);

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            var data = new List<AntivirusChangeAudit>();
            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/AntivirusChangeAudit/" + UCode + "");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/TableChangesAudit/AntivirusChangeAudit/" + UCode + "");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusChangeAudit>>(content) : null;


                    // datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(data);
                }
                return Json(data);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetailsJson(string domain)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserDetails");

                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<List<UserDetails>>(content);
                    var userDetail = data?.FirstOrDefault(x => x.domainName == domain);

                    if (userDetail != null)
                    {
                        return Json(new
                        {
                            success = true,
                            ipAddress = userDetail.IpAddress ?? "N/A"
                        });
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
                string[] parts = domain.Split('-');
                string UCode = parts[parts.Length - 1];

                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/DiskUsage");

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

                        return Json(new
                        {
                            success = true,
                            usagePercent = usagePercent
                        });
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
                string[] parts = domain.Split('-');
                string UCode = parts[parts.Length - 1];

                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/Summary");

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
                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/Command/GetConnectedDevices");

                var response = await httpClient.GetAsync("");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return Json(new List<string>());
                    }

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



        private string GetUCodeFromDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return "";
            var parts = domain.Split('-');
            return parts.Length > 1 ? parts[1] : domain;
        }
    }
}
