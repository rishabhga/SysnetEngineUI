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
            return View();
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
    }
}
