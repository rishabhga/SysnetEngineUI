using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ManageEngineWebApp.Attributes;

namespace ManageEngineWebApp.Controllers
{
    public class ServiceDeskController : Controller
    {
        private readonly string _baseUrl;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ServiceDeskController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7225";
            //_baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://172.16.15.15:4431";
        }

        private System.Net.Http.HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");
        private bool HasPermission(string permissionCode) => RoleHelper.HasPermission(HttpContext, permissionCode);
        private bool IsTopLevelAdmin() => RoleHelper.IsTopLevelAdmin(HttpContext);
        private (int? companyId, int? groupId, int? locationId) GetUserScope()
        {
            if (IsTopLevelAdmin()) return (null, null, null);
            return (RoleHelper.GetCompanyId(HttpContext), 
                    RoleHelper.GetGroupId(HttpContext), 
                    RoleHelper.GetLocationId(HttpContext));
        }
        private string BuildScopedQuery(int? requestedCompanyId, int? requestedLocationId, int? requestedGroupId = null)
        {
            var (userCompanyId, userGroupId, userLocationId) = GetUserScope();
            var companyId = userCompanyId ?? requestedCompanyId;
            var locationId = userLocationId ?? requestedLocationId;
            var groupId = userGroupId ?? requestedGroupId;
            
            var q = new List<string>();
            if (companyId.HasValue) q.Add($"companyId={companyId}");
            if (locationId.HasValue) q.Add($"locationId={locationId}");
            if (groupId.HasValue) q.Add($"groupId={groupId}");
            
            return q.Any() ? "?" + string.Join("&", q) : "";
        }

        [AuthFilter]
        [DynamicPermission("ServiceDesk.View", "View Dashboard")]
        public IActionResult Index()
        {
            SetViewPermissions();
            return View("Dashboard");
        }
        private void SetViewPermissions()
        {
            ViewBag.CanCreate = HasPermission("ServiceDesk.Create") || IsTopLevelAdmin();
            ViewBag.CanAssign = HasPermission("ServiceDesk.Assign") || IsTopLevelAdmin();
            ViewBag.CanApprove = HasPermission("ServiceDesk.Approve") || IsTopLevelAdmin();
            ViewBag.CanDelete = HasPermission("ServiceDesk.Delete") || IsTopLevelAdmin();
            ViewBag.IsSuperAdmin = IsTopLevelAdmin();
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetStats(int? companyId, int? locationId, int? groupId)
        {
            try
            {
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Stats{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetTickets(int? companyId, int? locationId, int? groupId, string? clientId)
        {
            try
            {
                var query = BuildScopedQuery(companyId, locationId, groupId);
                if (!string.IsNullOrEmpty(clientId))
                    query += (string.IsNullOrEmpty(query) ? "?" : "&") + $"clientId={clientId}";

                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.Create", "Create Ticket")]
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
        public async Task<IActionResult> GetPriorities()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Priorities");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetStatuses()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Statuses");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetSLAConfigs()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/SLAConfigs");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetEngineers(int? companyId, int? locationId, int? groupId)
        {
            try
            {
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Engineers{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [AuthFilter]
        public IActionResult Tickets()
        {
            SetViewPermissions();
            return View();
        }

        [AuthFilter]
        [DynamicPermission("ServiceDesk.Create", "Create Ticket")]
        public IActionResult CreateTicket()
        {
            ViewBag.CanCreate = HasPermission("ServiceDesk.Create") || IsTopLevelAdmin();
            return View();
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.Assign", "Assign Ticket")]
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
        [DynamicPermission("ServiceDesk.Edit", "Update Ticket Details")]
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
        [DynamicPermission("ServiceDesk.Edit", "Update Ticket Status")]
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
        [DynamicPermission("ServiceDesk.Approve", "Approve Ticket")]
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
        [DynamicPermission("ServiceDesk.Approve", "Reject Ticket")] 
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
                SetViewPermissions();
                return View(ticket);
            }
            catch { return NotFound(); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/CompaniesDetails/Companiesdata");
                var json = await response.Content.ReadAsStringAsync();

                // Scope filter: restrict to user's assigned company
                var (userCompanyId, _, _) = GetUserScope();
                if (userCompanyId.HasValue)
                {
                    var companies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(json);
                    if (companies != null)
                    {
                        companies = companies.Where(c => (int)c.id == userCompanyId.Value).ToList();
                        return Json(companies);
                    }
                }
                return Content(json, "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetLocations(int companyId, int? groupId)
        {
            try
            {
                // Scope check: ensure user can access this company
                var (userCompanyId, userGroupId, _) = GetUserScope();
                var effectiveCompanyId = userCompanyId ?? companyId;
                var effectiveGroupId = userGroupId ?? groupId;

                var url = $"{_baseUrl}/api/CompaniesDetails/Locationdata?comid={effectiveCompanyId}";
                if(effectiveGroupId.HasValue) url += $"&groupid={effectiveGroupId}";
                
                var response = await GetClient().GetAsync(url);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetUsersByLocation(int companyId, int groupId, int locationId)
        {
            try
            {
                // Scope check: enforce user's scope
                var (userCompanyId, userGroupId, userLocationId) = GetUserScope();
                var effectiveCompanyId = userCompanyId ?? companyId;
                var effectiveGroupId = userGroupId ?? groupId;
                var effectiveLocationId = userLocationId ?? locationId;

                var response = await GetClient().GetAsync($"{_baseUrl}/api/WindowsUserDetails/allUser?comId={effectiveCompanyId}&groupid={effectiveGroupId}&locationId={effectiveLocationId}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetGroups(int companyId)
        {
            try
            {
                // Scope check: restrict to user's company
                var (userCompanyId, _, _) = GetUserScope();
                var effectiveCompanyId = userCompanyId ?? companyId;

                var response = await GetClient().GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata?id={effectiveCompanyId}");
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

        [HttpDelete]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.Delete", "Delete Ticket")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            try
            {
                var username = HttpContext.Session.GetString("username") ?? "System";
                var response = await GetClient().DeleteAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{id}?username={username}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> StartWork()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/StartWork", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> ResolveTicket()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Resolve", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ================= MASTER PARTS INVENTORY =================

        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageParts", "Manage Parts Inventory")] 
        public IActionResult PartsInventory()
        {
            return View();
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetMasterParts()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/MasterParts");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> AddMasterPart()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/MasterParts", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdateMasterPart()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/UpdateMasterPart", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeleteMasterPart(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeleteMasterPart?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
    }
}
