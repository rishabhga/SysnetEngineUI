using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ManageEngineWebApp.Controllers
{
    public class ServiceDeskController : Controller
    {
        private readonly string _baseUrl;
        private readonly IConfiguration _configuration;

        public ServiceDeskController(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
        }

        private System.Net.Http.HttpClient GetClient()
        {
            var handler = new System.Net.Http.HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            return new System.Net.Http.HttpClient(handler);
        }

        [AuthFilter]
        public IActionResult Index()
        {
            return View("Dashboard");
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetStats(int? companyId, int? locationId)
        {
            try
            {
                var query = $"?companyId={companyId}&locationId={locationId}";
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Stats{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetTickets(int? companyId, int? locationId)
        {
            try
            {
                var query = $"?companyId={companyId}&locationId={locationId}";
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SaveTicket()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Categories");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetEngineers(int? companyId, int? locationId)
        {
            try
            {
                var query = $"?companyId={companyId}&locationId={locationId}";
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Engineers{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [AuthFilter]
        public IActionResult Tickets()
        {
            return View();
        }

        [AuthFilter]
        public IActionResult CreateTicket()
        {
            return View();
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> AssignTicket()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Assign", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdateTicketDetails()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/UpdateDetails", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdateStatus()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/UpdateStatus", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> ApproveTicket()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Approve", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> RejectTicket()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Reject", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> AddPart()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/AddPart", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetParts(int ticketId)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{ticketId}/Parts");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpDelete]
        [AuthFilter]
        public async Task<IActionResult> DeletePart(int id)
        {
            try
            {
                var response = await GetClient().DeleteAsync($"{_baseUrl}/api/ServiceDesk/Parts/{id}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [AuthFilter]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{id}");
                if (!response.IsSuccessStatusCode) return NotFound();
                
                var content = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ticket = System.Text.Json.JsonSerializer.Deserialize<ManageEngineWebApp.Models.HelpdeskTicket>(content, options);
                return View(ticket);
            }
            catch { return NotFound(); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetLocations()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Locations");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetUsersByLocation(int locationId)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/UsersByLocation/{locationId}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }
        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetComments(int ticketId)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{ticketId}/Comments");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> AddComment()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Comments", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetAttachments(int ticketId)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{ticketId}/Attachments");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> AddAttachment(int ticketId)
        {
            try
            {
                var file = Request.Form.Files[0];
                if (file == null || file.Length == 0) return Json(new { success = false, message = "No file uploaded" });

                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                
                content.Add(fileContent, "File", file.FileName);
                content.Add(new StringContent(ticketId.ToString()), "TicketId");
                
                var username = HttpContext.Session.GetString("username") ?? "System";
                var userId = HttpContext.Session.GetString("userId") ?? "0";
                
                content.Add(new StringContent(username), "UploadedBy");
                content.Add(new StringContent(userId), "UploadedById");

                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Attachments", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [AuthFilter]
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetLogs(int ticketId)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{ticketId}/Logs");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }
    }
}
