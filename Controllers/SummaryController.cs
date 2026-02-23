using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;

namespace ManageEngineWebApp.Controllers
{
    public class SummaryController : Controller
    {

        private readonly HttpClient _httpClient;

        public SummaryController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<IActionResult> Index()
        {

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (var httpClient = new HttpClient(handler))
            {


               

               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsUserDetails");
                httpClient.BaseAddress = new Uri("https://Localhost:7225/api/WindowsUserDetails/allUser"); // Replace with your server's URL

                //var jsonContent = JsonConvert.SerializeObject(systemInfometion);
                //var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send POST request to the server
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                var datalist = data != null ? data.Where(x => x.Status == "Enabled").ToList() : new List<WindowsUserDetails>();
                return View(datalist);
            }

                    //if (!response.IsSuccessStatusCode)
                    //{
                    //    throw new Exception($"Failed to send data. Server responded with: {response.StatusCode}");
                    //}
            }


            //var response = await _httpClient.GetAsync("https://localhost:7225/api/WindowsUserDetails/allUser");
            // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsUserDetails");


            //return View();
            throw new Exception("Unable to fetch data from the API.");
        }


        public List<WindowsUserDetails> datalist { get; set; }

        [HttpGet]
        public async Task<IActionResult> AllDatapage(string domain)
        {

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {


               

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");
               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsUserDetails");
                

                

                // Send POST request to the server
                var response = await httpClient.GetAsync("");
                

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    datalist = data != null ? data.Where(x => x.DomainName == domain).ToList() : new List<WindowsUserDetails>();

                    if (datalist.Any())
                    {
                        ViewBag.lastScan = datalist[0].DateTime;
                        ViewBag.UserName = datalist[0].DomainName;
                        ViewBag.LastLogUser = datalist[0].UserName;
                        ViewBag.LastBootTime = datalist[0].DateTime;
                    }

                    return View(datalist);
                }



                return View(datalist);


                    //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

                    //if (response.IsSuccessStatusCode)
                    //{
                    //    var content = await response.Content.ReadAsStringAsync();
                    //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    //     datalist = data.Where(x => x.DomainName == domain).ToList();

                    //    ViewBag.lastScan = datalist[0].DateTime;
                    //    ViewBag.UserName = datalist[0].DomainName;
                    //    ViewBag.LastLogUser = datalist[3].UserName;
                    //    ViewBag.LastBootTime = datalist[0].DateTime;


                    //    return View(datalist);
            }

            //throw new Exception("Unable to fetch data from the API.");
        }
        [HttpGet]
        public async Task<IActionResult> services(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {
 

               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsService");
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsService");
                
                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsService>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowsService>();
                    return Json(resultList);
                }

               // return View(datalist);
            }

            return Json(datalist);




        }



        public async Task<IActionResult> Summary(string domain)
        {
            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;



            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/Summary");


            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Summary>>(content) : null;
            //    var datalist = data.Where(x =>x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}
            using (var httpClient = new HttpClient(handler))
            {




                httpClient.BaseAddress = new Uri("https://localhost:7225/api/Summary");
               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/Summary");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<Summary>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<Summary>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

        }

        public async Task<IActionResult> users(string domain)
        {



            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "DESKTOP-C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}


            using (var httpClient = new HttpClient(handler))
            {




                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");
               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsUserDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.DomainName == domain).ToList() : new List<WindowsUserDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }


        }

        //groups

        public async Task<IActionResult> groups(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
           // var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsGroupDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsGroupDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {




                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsGroupDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsGroupDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsGroupDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowsGroupDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //drivers
        public async Task<IActionResult> drivers(string domain)
        {
            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowDrivers");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}
            using (var httpClient = new HttpClient(handler))
            {




                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowDrivers");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowDrivers");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowDrivers>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<WindowDrivers>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //Share

        public async Task<IActionResult> Share(string domain)
        {


            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {




                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsUserDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == domain).ToList() : new List<WindowsUserDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

            
        }

        //Battery

        public async Task<IActionResult> Battery(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/WindowsUserDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/WindowsUserDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
                    if (data != null && data.Count > 0)
                    {
                        var resultList = data.Where(x => x.UserCode == UCode).ToList();
                        return Json(resultList);
                    }
                    else
                    {
                        return Json(null);
                    }
                }
                return Json(datalist);

            }


        }

        //BIOS

        public async Task<IActionResult> BIOS(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };



            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/BIOSDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/BIOSDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BIOSDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<BIOSDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/BIOSDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<BIOSDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            
        }

        //HardDisk
        public async Task<IActionResult> HardDisk(string domain)
        {


            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/HardDiskDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<HardDiskDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/HardDiskDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/HardDiskDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<HardDiskDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<HardDiskDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
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
            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/MotherboardDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MotherboardDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MotherboardDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/MotherboardDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MotherboardDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<MotherboardDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
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


            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (var httpClient = new HttpClient(handler))
            {

               httpClient.BaseAddress = new Uri("https://localhost:7225/api/PhysicalMemoryDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/PhysicalMemoryDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PhysicalMemoryDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<PhysicalMemoryDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }




            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/PhysicalMemoryDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PhysicalMemoryDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            throw new Exception("Unable to fetch data from the API.");
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

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "T33QOLJ";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/ProcessorDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ProcessorDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

               httpClient.BaseAddress = new Uri("https://localhost:7225/api/ProcessorDetails");
               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/ProcessorDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ProcessorDetails>>(content) : null;
                    var resultList = data != null ? data.Where(x => x.UserCode == UCode).ToList() : new List<ProcessorDetails>();
                    return Json(resultList);
                }
                return Json(datalist);

            }

        }

        //Sound

        public async Task<IActionResult> Sound(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/SoundDeviceDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/USBHubDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SoundDeviceDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
            


            throw new Exception("Unable to fetch data from the API.");
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

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.BaseAddress = new Uri("https://localhost:7225/api/USBHubDetails");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/USBHubDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBHubDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
            //// string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == UCode).ToList();
            //    return Json(datalist);
            //}

            throw new Exception("Unable to fetch data from the API.");
        }

        //DesktopApps

        public async Task<IActionResult> DesktopApps(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/InstalledApplication");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/InstalledApplication");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }
        //MicrosoftstoreApps

        public async Task<IActionResult> MicrosoftstoreApps(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/InstalledApplication");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication");
                httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/InstalledApplication");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

           
        }
        //MeteredSoftware

        public async Task<IActionResult> MeteredSoftware(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/InstalledApplication");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/InstalledApplication");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/InstalledApplication");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<InstalledApplication>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        //UsbAudit

        public async Task<IActionResult> UsbDeviceAudit(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

           
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UsbDeviceInfo");
                //httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/UsbDeviceInfo");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBDeviceInfo>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        //AntivirusDetails

        public async Task<IActionResult> Antivirus(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            //string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/AntivirusDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
            //    var datalist = data.Where(x => x.UserCode == domain).ToList();
            //    return Json(datalist);
            //}

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/AntivirusDetails");
                httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }
        //
        public async Task<IActionResult> Firewall(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            

            using (var httpClient = new HttpClient(handler))
            {

                //httpClient.BaseAddress = new Uri("https://localhost:7225/api/AntivirusDetails");
                httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<AntivirusDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        // CustomComputerDetails
        public async Task<IActionResult> CustomComputerDetails(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/CustomComputerDetails");
               // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<CustomComputerDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        // DeviceSummary
        public async Task<IActionResult> DeviceSummary(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/DeviceSummary");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceSummary>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        // OSSummary
        public async Task<IActionResult> OSSummary(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/OSSummary");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<OSSummary>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        //SecurityPrivacyDetails

        public async Task<IActionResult> SecurityPrivacyDetails(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/SecurityPrivacyDetails");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SecurityPrivacyDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        //ApplicationSettings

        public async Task<IActionResult> ApplicationSettings(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/ApplicationSettings");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<ApplicationSettings>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        //SocialSearchSettings

        public async Task<IActionResult> SocialSearchSettings(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/SocialSearchSettings");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<SocialSearchSettings>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        // RestrictionOnDevice

        public async Task<IActionResult> RestrictionOnDevice(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/DeviceRestrictionDetails");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<DeviceRestrictionDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        // MonitorInfo

        public async Task<IActionResult> Monitor(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/MonitorInfo");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<MonitorInfo>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }

        // NetworkAdapterDetails

        public async Task<IActionResult> NetworkAdapters(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/NetworkAdapterDetails");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<NetworkAdapterDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }


        // KeyboardDetails

        public async Task<IActionResult> Keyboard(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };


            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/KeyboardDetails");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<KeyboardDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }
        }
        //Printers

        public async Task<IActionResult> Printers(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            ////string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == UCode).ToList();
            //    return Json(datalist);
            //}
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/PrinterDetails");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PrinterDetails>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }




        //PointingDeviceInfo


        public async Task<IActionResult> PointingDevices(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            ////string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == UCode).ToList();
            //    return Json(datalist);
            //}
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/PointingDeviceInfo");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<PointingDeviceInfo>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        //VideoDeviceInfo

        public async Task<IActionResult> VideoControllers(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            ////string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == UCode).ToList();
            //    return Json(datalist);
            //}
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/VideoDeviceInfo");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<VideoDeviceInfo>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //USBControllerInfo

        public async Task<IActionResult> USBControllers(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            ////string domain = "C60AEFI";
            //var response = await _httpClient.GetAsync("https://172.16.15.30:4431/api/WindowsUserDetails");

            //if (response.IsSuccessStatusCode)
            //{
            //    var content = await response.Content.ReadAsStringAsync();
            //    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<WindowsUserDetails>>(content) : null;
            //    var datalist = data.Where(x => x.DomainName == UCode).ToList();
            //    return Json(datalist);
            //}
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/USBControllerInfo");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<USBControllerInfo>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }

        //AuditHistory
        public async Task<IActionResult> AuditHistory(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
           
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserAuditHistory");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserAuditHistory>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


        //LoginHistory

        public async Task<IActionResult> LoginHistory(string domain)
        {

            string Addedtokennumber = domain.Split('-')[1];
            string UCode = Addedtokennumber;

            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
            
            using (var httpClient = new HttpClient(handler))
            {

                httpClient.BaseAddress = new Uri("https://localhost:7225/api/UserLogonHistory");
                // httpClient.BaseAddress = new Uri("https://172.16.15.30:4431/api/AntivirusDetails");

                var response = await httpClient.GetAsync("");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = !string.IsNullOrEmpty(content) ? JsonConvert.DeserializeObject<List<UserLogonHistory>>(content) : null;
                    var datalist = data.Where(x => x.UserCode == UCode).ToList();
                    return Json(datalist);
                }
                return Json(datalist);

            }

            throw new Exception("Unable to fetch data from the API.");
        }


    }
}
