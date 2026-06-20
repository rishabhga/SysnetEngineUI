using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ManageEngineWebApp.Datacontext;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class SummaryController : BaseController
    {
        public SummaryController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
            : base(httpClientFactory, configuration)
        {
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var httpClient = GetClient();
                var query = BuildScopedQuery();
                var response = await httpClient.GetAsync($"api/WindowsUserDetails/allUser{query}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    var localDatalist = data != null ? data.Where(x => x.Status == "Enabled").ToList() : new List<WindowsUserDetails>();
                    return View(localDatalist);
                }

                ViewBag.ErrorMessage = $"Unable to fetch data from the API (Status: {response.StatusCode}).";
                return View(new List<WindowsUserDetails>());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Connection Error: Unable to reach the backend service.";
                System.Diagnostics.Debug.WriteLine($"Summary Index Error: {ex.Message}");
                return View(new List<WindowsUserDetails>());
            }
        }


        // Removed datalist global property for thread safety. Use local variables.

        [HttpGet]
        public async Task<IActionResult> AllDatapage(string domain)
        {
            var localDatalist = new List<WindowsUserDetails>();
            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    localDatalist = data != null ? data.Where(x => x.DomainName == domain).ToList() : new List<WindowsUserDetails>();

                    if (localDatalist.Any())
                    {
                        var machine = localDatalist[0];
                        if (!RoleHelper.ValidateScope(HttpContext, machine.CompanyId, machine.GroupId, machine.LocationId))
                        {
                            return RedirectToAction("Index", "Home");
                        }

                        ViewBag.lastScan = localDatalist[0].DateTime;
                        ViewBag.UserName = localDatalist[0].DomainName;
                        ViewBag.LastLogUser = localDatalist[0].UserName;
                        ViewBag.LastBootTime = localDatalist[0].DateTime;
                        return View(localDatalist);
                    }
                }
            }
            catch (Exception) { }
            return View(localDatalist);
        }

        [HttpGet]
        public async Task<IActionResult> services(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsService");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsService>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowsService>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<WindowsService>());
        }



        public async Task<IActionResult> Summary(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/Summary");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Summary>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<Summary>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<Summary>());
        }

        public async Task<IActionResult> users(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.DomainName == domain).ToList() : new List<WindowsUserDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<WindowsUserDetails>());
        }

        //groups

        public async Task<IActionResult> groups(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsGroupDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsGroupDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowsGroupDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<WindowsGroupDetails>());
        }

        //drivers
        public async Task<IActionResult> drivers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowDrivers");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowDrivers>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<WindowDrivers>());
        }

        //Share

        public async Task<IActionResult> Share(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.DomainName == domain).ToList() : new List<WindowsUserDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<WindowsUserDetails>());
        }

        //Battery

        public async Task<IActionResult> Battery(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/WindowsUserDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowsUserDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<WindowsUserDetails>());
        }

        //BIOS

        public async Task<IActionResult> BIOS(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/BIOSDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BIOSDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<BIOSDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<BIOSDetails>());
        }

        //HardDisk
        public async Task<IActionResult> HardDisk(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/HardDiskDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<HardDiskDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<HardDiskDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<HardDiskDetails>());
        }

        //Keyboard

        //public async Task<IActionResult> Keyboard(string domain)
        //{

        //    string Addedtokennumber = domain.Split('-')[1];
        //    string UCode = Addedtokennumber;

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    //string domain = "C60AEFI";
        //    var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
        //        var datalist = data.Where(x => x.DomainName == UCode).ToList();
        //        return Json(datalist);
        //    }

        //    throw new Exception("Unable to fetch data from the API.");
        //}

        //Monitor
        //public async Task<IActionResult> Monitor(string domain)
        //{


        //    string Addedtokennumber = domain.Split('-')[1];
        //    string UCode = Addedtokennumber;
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    //string domain = "C60AEFI";
        //    var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
        //        var datalist = data.Where(x => x.DomainName == UCode).ToList();
        //        return Json(datalist);
        //    }

        //    throw new Exception("Unable to fetch data from the API.");
        //}

        //Motherboard

        public async Task<IActionResult> Motherboard(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/MotherboardDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MotherboardDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<MotherboardDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<MotherboardDetails>());
        }


        //NetworkAdapters
        //public async Task<IActionResult> NetworkAdapters(string domain)
        //{

        //    string Addedtokennumber = domain.Split('-')[1];
        //    string UCode = Addedtokennumber;

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };
        //    //string domain = "C60AEFI";
        //    var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
        //        var datalist = data.Where(x => x.DomainName == UCode).ToList();
        //        return Json(datalist);
        //    }

        //    throw new Exception("Unable to fetch data from the API.");
        //}

        //PhysicalMemory

        public async Task<IActionResult> PhysicalMemory(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                httpClient.BaseAddress = new Uri($"{_baseUrl}/api/PhysicalMemoryDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PhysicalMemoryDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<PhysicalMemoryDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<PhysicalMemoryDetails>());
        }


        ////PointingDevices

        //public async Task<IActionResult> PointingDevices(string domain)
        //{


        //    string Addedtokennumber = domain.Split('-')[1];
        //    string UCode = Addedtokennumber;
        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    // string domain = "C60AEFI";
        //    var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
        //        var datalist = data.Where(x => x.DomainName == UCode).ToList();
        //        return Json(datalist);
        //    }

        //    throw new Exception("Unable to fetch data from the API.");
        //}



        //Processors

        public async Task<IActionResult> Processors(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/ProcessorDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ProcessorInfo>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<ProcessorInfo>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<ProcessorInfo>());
        }

        //Sound

        public async Task<IActionResult> Sound(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/SoundDeviceDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoundDeviceDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<SoundDeviceDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<SoundDeviceDetails>());
        }

        //USBControllers
        //public async Task<IActionResult> USBControllers(string domain)
        //{

        //    string Addedtokennumber = domain.Split('-')[1];
        //    string UCode = Addedtokennumber;

        //    HttpClientHandler handler = new HttpClientHandler
        //    {
        //        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        //    };

        //    //string domain = "C60AEFI";
        //    var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
        //        var datalist = data.Where(x => x.DomainName == UCode).ToList();
        //        return Json(datalist);
        //    }

        //    throw new Exception("Unable to fetch data from the API.");
        //}

        //USBHub

        public async Task<IActionResult> USBHub(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/USBHubDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBHubDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<USBHubDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<USBHubDetails>());
        }

        //DesktopApps

        public async Task<IActionResult> DesktopApps(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/InstalledApplication");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<InstalledApplication>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<InstalledApplication>());
        }
        //MicrosoftstoreApps

        public async Task<IActionResult> MicrosoftstoreApps(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/InstalledApplication");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<InstalledApplication>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<InstalledApplication>());
        }
        //MeteredSoftware

        public async Task<IActionResult> MeteredSoftware(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/InstalledApplication");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<InstalledApplication>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<InstalledApplication>());
        }

        //UsbAudit

        public async Task<IActionResult> UsbDeviceAudit(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/UsbDeviceInfo");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBDeviceInfo>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<USBDeviceInfo>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<USBDeviceInfo>());
        }


        //AntivirusDetails

        public async Task<IActionResult> Antivirus(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/AntivirusDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<AntivirusDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<AntivirusDetails>());
        }
        //
        public async Task<IActionResult> Firewall(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/AntivirusDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<AntivirusDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<AntivirusDetails>());
        }

        // CustomComputerDetails
        public async Task<IActionResult> CustomComputerDetails(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/CustomComputerDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<CustomComputerDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<CustomComputerDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<CustomComputerDetails>());
        }

        // DeviceSummary
        public async Task<IActionResult> DeviceSummary(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/DeviceSummary");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceSummary>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<DeviceSummary>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<DeviceSummary>());
        }


        // OSSummary
        public async Task<IActionResult> OSSummary(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/OSSummary");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummary>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<OSSummary>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<OSSummary>());
        }


        //SecurityPrivacyDetails

        public async Task<IActionResult> SecurityPrivacyDetails(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/SecurityPrivacyDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SecurityPrivacyDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<SecurityPrivacyDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<SecurityPrivacyDetails>());
        }


        //ApplicationSettings

        public async Task<IActionResult> ApplicationSettings(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/ApplicationSettings");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ApplicationSettings>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<ApplicationSettings>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<ApplicationSettings>());
        }

        //SocialSearchSettings

        public async Task<IActionResult> SocialSearchSettings(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/SocialSearchSettings");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SocialSearchSettings>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<SocialSearchSettings>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<SocialSearchSettings>());
        }

        // RestrictionOnDevice

        public async Task<IActionResult> RestrictionOnDevice(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/DeviceRestrictionDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceRestrictionDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<DeviceRestrictionDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<DeviceRestrictionDetails>());
        }


        // MonitorInfo

        public async Task<IActionResult> Monitor(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/MonitorInfo");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MonitorInfo>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<MonitorInfo>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<MonitorInfo>());
        }

        // NetworkAdapterDetails

        public async Task<IActionResult> NetworkAdapters(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/NetworkAdapterDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<NetworkAdapterDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<NetworkAdapterDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<NetworkAdapterDetails>());
        }


        // KeyboardDetails

        public async Task<IActionResult> Keyboard(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/KeyboardDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<KeyboardDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<KeyboardDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<KeyboardDetails>());
        }
        //Printers

        public async Task<IActionResult> Printers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/PrinterDetails");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PrinterDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<PrinterDetails>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<PrinterDetails>());
        }




        //PointingDeviceInfo


        public async Task<IActionResult> PointingDevices(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/PointingDeviceInfo");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PointingDeviceInfo>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<PointingDeviceInfo>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<PointingDeviceInfo>());
        }


        //VideoDeviceInfo

        public async Task<IActionResult> VideoControllers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/VideoDeviceInfo");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<VideoDeviceInfo>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<VideoDeviceInfo>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<VideoDeviceInfo>());
        }

        //USBControllerInfo

        public async Task<IActionResult> USBControllers(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/USBControllerInfo");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBControllerInfo>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<USBControllerInfo>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<USBControllerInfo>());
        }

        //AuditHistory
        public async Task<IActionResult> AuditHistory(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/UserAuditHistory");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserAuditHistory>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<UserAuditHistory>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<UserAuditHistory>());
        }


        //LoginHistory

        public async Task<IActionResult> LoginHistory(string domain)
        {
            if (!await IsDeviceAuthorized(domain)) return Forbid();

            try
            {
                string UCode = domain;
                using var httpClient = GetClient();
                var response = await httpClient.GetAsync($"{_baseUrl}/api/UserLogonHistory");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserLogonHistory>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<UserLogonHistory>();
                    return Json(resultList);
                }
            }
            catch (Exception) { }
            return Json(new List<UserLogonHistory>());
        }


    }
}
