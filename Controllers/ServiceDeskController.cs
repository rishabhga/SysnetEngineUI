using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ManageEngineWebApp.Attributes;
using Newtonsoft.Json;
using ManageEngineWebApp.Models;

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
            _baseUrl = _configuration["ApiSettings:BaseUrl"];
        }

        private System.Net.Http.HttpClient GetClient() => _httpClientFactory.CreateClient("ManageEngineApi");
        private bool HasPermission(string permissionCode) => RoleHelper.HasPermission(HttpContext, permissionCode);
        private bool IsTopLevelAdmin() => RoleHelper.IsTopLevelAdmin(HttpContext);
        private (List<int> companyIds, List<int> groupIds, List<int> locationIds) GetUserScope()
        {
            if (IsTopLevelAdmin()) return (new List<int>(), new List<int>(), new List<int>());
            return (RoleHelper.GetCompanyIds(HttpContext), 
                    RoleHelper.GetGroupIds(HttpContext), 
                    RoleHelper.GetLocationIds(HttpContext));
        }

        private string BuildScopedQuery(int? requestedCompanyId, int? requestedLocationId, int? requestedGroupId = null)
        {
            var (userCompanyIds, userGroupIds, userLocationIds) = GetUserScope();
            
            var companyIds = requestedCompanyId.HasValue ? new List<int> { requestedCompanyId.Value } : userCompanyIds;
            var locationIds = requestedLocationId.HasValue ? new List<int> { requestedLocationId.Value } : userLocationIds;
            var groupIds = requestedGroupId.HasValue ? new List<int> { requestedGroupId.Value } : userGroupIds;
            
            var q = new List<string>();
            foreach (var id in companyIds) q.Add($"companyId={id}");
            foreach (var id in locationIds) q.Add($"locationId={id}");
            foreach (var id in groupIds) q.Add($"groupId={id}");
            
            // If restricted user has no assigned scope, force empty result with invalid ID
            if (!IsTopLevelAdmin() && !q.Any())
            {
                q.Add("comId=-1");
            }

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
            ViewBag.CanCreate = HasPermission("ServiceDesk.Create");
            ViewBag.CanAssign = HasPermission("ServiceDesk.Assign");
            ViewBag.CanApprove = HasPermission("ServiceDesk.Approve");
            ViewBag.CanDelete = HasPermission("ServiceDesk.Delete");
            ViewBag.CanManageSLA = HasPermission("ServiceDesk.ManageSLA");
            ViewBag.CanManageMasterParts = HasPermission("ServiceDesk.ManageMasterParts");
            ViewBag.CanManageParts = HasPermission("ServiceDesk.ManageParts");

            ViewBag.CanAdminSettings = HasPermission("ServiceDesk.AdminSettings");
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
        public async Task<IActionResult> GetTickets(int? companyId, int? locationId, int? groupId, string? clientId, string status = "all", int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                var queryParams = BuildScopedQuery(companyId, locationId, groupId);
                var sep = string.IsNullOrEmpty(queryParams) ? "?" : "&";
                var fullQuery = $"{queryParams}";
                
                if (!string.IsNullOrEmpty(clientId))
                    fullQuery += (string.IsNullOrEmpty(fullQuery) ? "?" : "&") + $"clientId={clientId}";

                fullQuery += (string.IsNullOrEmpty(fullQuery) ? "?" : "&") + $"status={status}&pageNumber={pageNumber}&pageSize={pageSize}";

                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets{fullQuery}");
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { items = new List<object>(), totalItems = 0, totalPages = 0, currentPage = 1, error = ex.Message });
            }
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
        public async Task<IActionResult> Tickets()
        {
            try
            {
                var statusResponse = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Statuses");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusJson = await statusResponse.Content.ReadAsStringAsync();
                    var statuses = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(statusJson);
                    
                    var statusOrders = statuses.ToDictionary(
                        s => ((string)s.statusCode ?? (string)s.statusName).ToUpperInvariant(),
                        s => (int)s.sortOrder
                    );

                    var actionMappings = statuses.Where(s => !string.IsNullOrEmpty((string)s.systemAction))
                        .ToDictionary(
                            s => ((string)s.systemAction).ToUpperInvariant(),
                            s => (int)s.sortOrder
                        );
                    
                    ViewBag.StatusOrdersJson = Newtonsoft.Json.JsonConvert.SerializeObject(statusOrders);
                    ViewBag.SystemActionsJson = Newtonsoft.Json.JsonConvert.SerializeObject(actionMappings);
                    
                    var approvedStatus = statuses?.FirstOrDefault(s => (string)s.systemAction == "Approve" || (string)s.statusCode == "Approved" || (string)s.statusName == "Approved");
                    ViewBag.ApprovedStatusSortOrder = approvedStatus != null ? (int)approvedStatus.sortOrder : 2;
                }
            }
            catch { }

            SetViewPermissions();
            return View();
        }

        [AuthFilter]
        [DynamicPermission("ServiceDesk.Create", "Create Ticket")]
        public IActionResult CreateTicket()
        {
            ViewBag.CanCreate = HasPermission("ServiceDesk.Create");
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

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeletePart(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeletePart/{id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
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
                
                // Fetch approved status sort order
                var statusResponse = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Statuses");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusJson = await statusResponse.Content.ReadAsStringAsync();
                    var statuses = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(statusJson);
                    
                    var statusOrders = statuses.ToDictionary(
                        s => ((string)s.statusCode ?? (string)s.statusName).ToUpperInvariant(),
                        s => (int)s.sortOrder
                    );

                    var actionMappings = statuses.Where(s => !string.IsNullOrEmpty((string)s.systemAction))
                        .ToDictionary(
                            s => ((string)s.systemAction).ToUpperInvariant(),
                            s => (int)s.sortOrder
                        );

                    ViewBag.StatusOrdersJson = Newtonsoft.Json.JsonConvert.SerializeObject(statusOrders);
                    ViewBag.SystemActionsJson = Newtonsoft.Json.JsonConvert.SerializeObject(actionMappings);

                    var approvedStatus = statuses?.FirstOrDefault(s => (string)s.systemAction == "Approve" || (string)s.statusCode == "Approved" || (string)s.statusName == "Approved");
                    ViewBag.ApprovedStatusSortOrder = approvedStatus != null ? (int)approvedStatus.sortOrder : 2;
                }
                else
                {
                    ViewBag.ApprovedStatusSortOrder = 2;
                }

                SetViewPermissions();
                ViewBag.ApiBaseUrl = _baseUrl;
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

                if (!IsTopLevelAdmin())
                {
                    var (userCompanyIds, _, _) = GetUserScope();
                    var companies = JsonConvert.DeserializeObject<List<Companies>>(json);
                    if (companies != null)
                    {
                        companies = companies.Where(c => userCompanyIds.Contains(c.Id)).ToList();
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
                var (userCompanyIds, userGroupIds, _) = GetUserScope();
                var companyIds = companyId > 0 ? new List<int> { companyId } : userCompanyIds;
                var groupIds = groupId.HasValue && groupId.Value > 0 ? new List<int> { groupId.Value } : userGroupIds;

                var q = new List<string>();
                foreach (var id in companyIds) q.Add($"comId={id}");
                foreach (var id in groupIds) q.Add($"groupid={id}");
                var url = $"{_baseUrl}/api/CompaniesDetails/Locationdata" + (q.Any() ? "?" + string.Join("&", q) : "");
                
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
                var (userCompanyIds, userGroupIds, userLocationIds) = GetUserScope();
                var companyIds = companyId > 0 ? new List<int> { companyId } : userCompanyIds;
                var groupIds = groupId > 0 ? new List<int> { groupId } : userGroupIds;
                var locationIds = locationId > 0 ? new List<int> { locationId } : userLocationIds;

                var q = new List<string>();
                foreach (var id in companyIds) q.Add($"comId={id}");
                foreach (var id in groupIds) q.Add($"groupid={id}");
                foreach (var id in locationIds) q.Add($"locationId={id}");
                var url = $"{_baseUrl}/api/WindowsUserDetails/allUser" + (q.Any() ? "?" + string.Join("&", q) : "");

                var response = await GetClient().GetAsync(url);
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
                var (userCompanyIds, _, _) = GetUserScope();
                var companyIds = companyId > 0 ? new List<int> { companyId } : userCompanyIds;

                var q = new List<string>();
                foreach (var id in companyIds) q.Add($"id={id}");
                var url = $"{_baseUrl}/api/CompaniesDetails/Groupdata" + (q.Any() ? "?" + string.Join("&", q) : "");

                var response = await GetClient().GetAsync(url);
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

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> DownloadAttachment(string path, string name)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return NotFound();
                
                // Security: prevent directory traversal
                if (path.Contains("..") || path.Contains("~")) return BadRequest("Invalid path");
                
                var response = await GetClient().GetAsync($"{_baseUrl}{path}");
                if (!response.IsSuccessStatusCode) return NotFound();
                
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                return File(fileBytes, contentType, name ?? "download");
            }
            catch { return NotFound(); }
        }

        [AuthFilter]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index");
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

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.Delete", "Delete Ticket")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            try
            {
                var username = HttpContext.Session.GetString("username") ?? "System";
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeleteTicket/{id}?username={username}", null);
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


        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageParts", "Manage Parts Inventory")] 
        public IActionResult PartsInventory()
        {
            SetViewPermissions();
            return View();
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetInventoryParts()
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
        public async Task<IActionResult> AddInventoryPart()
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
        public async Task<IActionResult> UpdateInventoryPart()
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
        public async Task<IActionResult> DeleteInventoryPart(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeleteMasterPart?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }



        [HttpGet]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageSLA", "Manage SLAs")]
        public IActionResult SLAManagement()
        {
            SetViewPermissions();
            return View();
        }

        [HttpGet]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ViewSLAReport", "View SLA Breaches")]
        public async Task<IActionResult> GetSLABreaches(int? companyId, int? locationId, int? groupId, string? breachType)
        {
            try
            {
                var query = BuildScopedQuery(companyId, locationId, groupId);
                query += (string.IsNullOrEmpty(query) ? "?" : "&") + "pageNumber=1&pageSize=100";
                if (!string.IsNullOrEmpty(breachType)) query += $"&breachType={breachType}";

                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/SLABreaches{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ViewSLAReport", "View SLA Stats")]
        public async Task<IActionResult> GetSLABreachStats(int? companyId, int? locationId, int? groupId)
        {
            try
            {
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/SLABreaches/Stats{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetTicketSLADetails(int ticketId)
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{ticketId}/SLADetails");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageSLA", "Save SLA Config")]
        public async Task<IActionResult> SaveSLAConfig()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/SLAConfigs", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageSLA", "Delete SLA Config")]
        public async Task<IActionResult> DeleteSLAConfig(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeleteSLAConfig/{id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetAllSLAConfigs()
        {
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/SLAConfigs/All");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetDetailedEngineers(int? companyId, int? locationId, int? groupId)
        {
            try
            {
                var query = BuildScopedQuery(companyId, locationId, groupId);
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Engineers/Detailed{query}");
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> CheckSLABreaches()
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/CheckSLABreaches", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> SeedServiceDeskPermissions()
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/SeedServiceDeskPermissions", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "View Admin Settings")]
        public IActionResult AdminSettings()
        {
            SetViewPermissions();
            return View();
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetAllCategories()
        {
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Categories/All");
            return Content(response, "application/json");
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> CreateCategory()
        {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Categories", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdateCategory()
        {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Categories/Update", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Categories/Delete?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetAllPriorities()
        {
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Priorities/All");
            return Content(response, "application/json");
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> CreatePriority()
        {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Priorities", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdatePriority()
        {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Priorities/Update", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeletePriority(int id)
        {
            try {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Priorities/Delete?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }


        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetAllStatuses()
        {
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Statuses/All");
            return Content(response, "application/json");
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> CreateTicketStatus()
        {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Statuses", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> UpdateTicketStatus()
        {
            try {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Statuses/Update", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [AuthFilter]
        public async Task<IActionResult> DeleteTicketStatus(int id)
        {
            try {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Statuses/Delete?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }





        [AuthFilter]
        public IActionResult Reports()
        {
            SetViewPermissions();
            return View();
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetTicketTrends(int days = 30)
        {
            var query = BuildScopedQuery(null, null, null);
            var sep = string.IsNullOrEmpty(query) ? "?" : "&";
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Reports/TicketTrends{query}{sep}days={days}");
            return Content(response, "application/json");
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetCategoryBreakdown()
        {
            var query = BuildScopedQuery(null, null, null);
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Reports/CategoryBreakdown{query}");
            return Content(response, "application/json");
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetPriorityBreakdown()
        {
            var query = BuildScopedQuery(null, null, null);
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Reports/PriorityBreakdown{query}");
            return Content(response, "application/json");
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetEngineerPerformance()
        {
            var query = BuildScopedQuery(null, null, null);
            var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Reports/EngineerPerformance{query}");
            return Content(response, "application/json");
        }

    }
}
