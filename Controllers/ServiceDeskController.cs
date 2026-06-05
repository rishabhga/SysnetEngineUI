using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ManageEngineWebApp.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ManageEngineWebApp.Models;

namespace ManageEngineWebApp.Controllers
{
    public class ServiceDeskController : BaseController
    {
        public ServiceDeskController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
            : base(httpClientFactory, configuration)
        {
        }


        private bool ValidateInventoryScope(int? companyId, int? groupId, int? locationId)
        {
            if (IsTopLevelAdmin())
            {
                return true;
            }

            var (userCompanyIds, userGroupIds, userLocationIds) = GetUserScope();
            if (companyId.HasValue && userCompanyIds.Any() && !userCompanyIds.Contains(companyId.Value)) return false;
            if (groupId.HasValue && userGroupIds.Any() && !userGroupIds.Contains(groupId.Value)) return false;
            if (locationId.HasValue && userLocationIds.Any() && !userLocationIds.Contains(locationId.Value)) return false;

            return true;
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
            ViewBag.CanBypassWorkflow = HasPermission("ServiceDesk.BypassWorkflow");

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
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetTickets(int? companyId, int? locationId, int? groupId, string? clientId, string status = "all", int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                var fullQuery = BuildScopedQuery(companyId, locationId, groupId);
                var prefix = string.IsNullOrEmpty(fullQuery) ? "?" : "&";

                if (!string.IsNullOrEmpty(clientId))
                {
                    fullQuery += $"{prefix}clientId={clientId}";
                    prefix = "&";
                }

                fullQuery += $"{prefix}status={status}&pageNumber={pageNumber}&pageSize={pageSize}";

                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets{fullQuery}");
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { items = new List<object>(), totalItems = 0, totalPages = 0, currentPage = 1, error = "An internal server error occurred." });
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

                var role = HttpContext.Session.GetString("role") ?? "";
                if (IsTopLevelAdmin() || HasPermission("ServiceDesk.Approve") || role.EndsWith("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var ticketObj = JsonConvert.DeserializeObject<dynamic>(body);
                        if (ticketObj != null)
                        {
                            ticketObj.AutoApprove = true;
                            body = JsonConvert.SerializeObject(ticketObj);
                        }
                    }
                    catch { }
                }

                var (userCompanyIds, userGroupIds, userLocationIds) = GetUserScope();
                var ticketData = JsonConvert.DeserializeObject<dynamic>(body);
                if (ticketData != null && !IsTopLevelAdmin())
                {
                    int? cId = ticketData.companyId;
                    int? gId = ticketData.groupId;
                    int? lId = ticketData.locationId;

                    if (cId.HasValue && userCompanyIds.Any() && !userCompanyIds.Contains(cId.Value))
                        return BadRequest(new { success = false, message = "Access Denied: Invalid Company selection." });
                    if (gId.HasValue && userGroupIds.Any() && !userGroupIds.Contains(gId.Value))
                        return BadRequest(new { success = false, message = "Access Denied: Invalid Group selection." });
                    if (lId.HasValue && userLocationIds.Any() && !userLocationIds.Contains(lId.Value))
                        return BadRequest(new { success = false, message = "Access Denied: Invalid Location selection." });
                }

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var query = BuildScopedQuery();
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Categories{query}");
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
                var query = BuildScopedQuery();
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Priorities{query}");
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
                var query = BuildScopedQuery();
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Statuses{query}");
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
                var query = BuildScopedQuery();
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/SLAConfigs{query}");
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
                    var statusOrders = statuses
                        .Where(s => s != null && (!string.IsNullOrEmpty((string)s.statusCode) || !string.IsNullOrEmpty((string)s.statusName)))
                        .ToDictionary(
                            s => ((string)s.statusCode ?? (string)s.statusName).ToUpperInvariant(),
                            s => (int)(s.sortOrder ?? 0)
                        );

                    var closedStatuses = statuses
                        .Where(s => s != null && s.isClosedState != null && (bool)s.isClosedState == true)
                        .Select(s => ((string)s.statusCode ?? (string)s.statusName ?? "").ToUpperInvariant())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    var actionMappings = statuses
                        .Where(s => s != null && !string.IsNullOrEmpty((string)s.systemAction))
                        .ToDictionary(
                            s => ((string)s.systemAction).ToUpperInvariant(),
                            s => (int)(s.sortOrder ?? 0)
                        );

                    ViewBag.StatusOrdersJson = Newtonsoft.Json.JsonConvert.SerializeObject(statusOrders);
                    ViewBag.SystemActionsJson = Newtonsoft.Json.JsonConvert.SerializeObject(actionMappings);
                    ViewBag.ClosedStatusesJson = Newtonsoft.Json.JsonConvert.SerializeObject(closedStatuses);

                    var approvedStatus = statuses?.FirstOrDefault(s => (string)s.systemAction == "Approve" || (string)s.statusCode == "Approved" || (string)s.statusName == "Approved");
                    ViewBag.ApprovedStatusSortOrder = approvedStatus != null ? (int)approvedStatus.sortOrder : 2;
                }
            }
            catch
            {
                // Ensure ViewBag always has valid JSON defaults so the page JS doesn't crash
                ViewBag.StatusOrdersJson = ViewBag.StatusOrdersJson ?? "{}";
                ViewBag.SystemActionsJson = ViewBag.SystemActionsJson ?? "{}";
                ViewBag.ClosedStatusesJson = ViewBag.ClosedStatusesJson ?? "[]";
                ViewBag.ApprovedStatusSortOrder = ViewBag.ApprovedStatusSortOrder ?? 2;
            }

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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageParts", "Add Ticket Parts")]
        public async Task<IActionResult> AddPart()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var payload = JsonConvert.DeserializeObject<JObject>(body);
                var companyId = payload?["companyId"]?.Value<int?>() ?? payload?["CompanyId"]?.Value<int?>();
                var groupId = payload?["groupId"]?.Value<int?>() ?? payload?["GroupId"]?.Value<int?>();
                var locationId = payload?["locationId"]?.Value<int?>() ?? payload?["LocationId"]?.Value<int?>();
                if (!ValidateInventoryScope(companyId, groupId, locationId))
                {
                    return Forbid();
                }

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/AddPart", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
        [DynamicPermission("ServiceDesk.ManageParts", "Delete Ticket Parts")]
        public async Task<IActionResult> DeletePart(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeletePart/{id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }
        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> Details(int id = 0, string? q = null)
        {
            if (!string.IsNullOrEmpty(q))
            {
                var p = ManageEngineWebApp.Helpers.EncryptionHelper.DecryptParams(q);
                if (p.TryGetValue("id", out var idStr) && int.TryParse(idStr, out var decId)) id = decId;
            }
            try
            {
                var response = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Tickets/{id}");
                if (!response.IsSuccessStatusCode) return NotFound();

                var content = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ticket = System.Text.Json.JsonSerializer.Deserialize<ManageEngineWebApp.Models.HelpdeskTicket>(content, options);
                var statusResponse = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/Statuses");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusJson = await statusResponse.Content.ReadAsStringAsync();
                    var statuses = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(statusJson);
                    var statusOrders = statuses
                        .Where(s => s != null && (!string.IsNullOrEmpty((string)s.statusCode) || !string.IsNullOrEmpty((string)s.statusName)))
                        .ToDictionary(
                            s => ((string)s.statusCode ?? (string)s.statusName).ToUpperInvariant(),
                            s => (int)(s.sortOrder ?? 0)
                        );

                    var closedStatuses = statuses
                        .Where(s => s != null && s.isClosedState != null && (bool)s.isClosedState == true)
                        .Select(s => ((string)s.statusCode ?? (string)s.statusName ?? "").ToUpperInvariant())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();

                    var actionMappings = statuses
                        .Where(s => s != null && !string.IsNullOrEmpty((string)s.systemAction))
                        .ToDictionary(
                            s => ((string)s.systemAction).ToUpperInvariant(),
                            s => (int)(s.sortOrder ?? 0)
                        );

                    ViewBag.StatusOrdersJson = Newtonsoft.Json.JsonConvert.SerializeObject(statusOrders);
                    ViewBag.SystemActionsJson = Newtonsoft.Json.JsonConvert.SerializeObject(actionMappings);
                    ViewBag.ClosedStatusesJson = Newtonsoft.Json.JsonConvert.SerializeObject(closedStatuses);

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
                    if (userCompanyIds.Any())
                    {
                        var companies = JsonConvert.DeserializeObject<List<Companies>>(json) ?? new List<Companies>();
                        companies = companies.Where(c => userCompanyIds.Contains(c.Id)).ToList();
                        return Json(companies);
                    }
                }

                return Content(json, "application/json");
            }
            catch { return Json(new List<Companies>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetLocations(int companyId, int? groupId)
        {
            try
            {
                if (!IsTopLevelAdmin() && companyId > 0 && !IsAuthorized(companyId))
                    return Json(new List<object>());

                var query = BuildScopedQuery(companyId > 0 ? companyId : (int?)null, null, groupId > 0 ? groupId : (int?)null);
                var response = await GetClient().GetAsync($"{_baseUrl}/api/CompaniesDetails/Locationdata{query}");
                var json = await response.Content.ReadAsStringAsync();

                var locations = JArray.Parse(json);
                if (groupId.HasValue && groupId.Value > 0)
                {
                    locations = new JArray(locations.Where(l =>
                    {
                        var lGroupId = l["groupsID"]?.Value<int?>()
                                    ?? l["GroupsID"]?.Value<int?>()
                                    ?? l["groupId"]?.Value<int?>()
                                    ?? l["GroupId"]?.Value<int?>()
                                    ?? 0;
                        return lGroupId == groupId.Value;
                    }));
                }
                else if (companyId > 0)
                {
                    locations = new JArray(locations.Where(l =>
                    {
                        var lCompId = l["companyID"]?.Value<int?>()
                                   ?? l["CompanyID"]?.Value<int?>()
                                   ?? l["companyId"]?.Value<int?>()
                                   ?? l["CompanyId"]?.Value<int?>()
                                   ?? 0;
                        return lCompId == companyId;
                    }));
                }
                if (!IsTopLevelAdmin())
                {
                    var (_, _, userLocationIds) = GetUserScope();
                    if (userLocationIds.Any())
                    {
                        locations = new JArray(locations.Where(l =>
                        {
                            var id = l["id"]?.Value<int>() ?? l["Id"]?.Value<int>() ?? 0;
                            return userLocationIds.Contains(id);
                        }));
                    }
                }

                return Content(locations.ToString(Newtonsoft.Json.Formatting.None), "application/json");
            }
            catch { return Json(new List<object>()); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> GetUsersByLocation(int companyId, int groupId, int locationId)
        {
            try
            {
                if (!IsTopLevelAdmin() && !IsAuthorized(companyId, groupId, locationId))
                    return Json(new List<object>());
                var query = BuildScopedQuery(companyId > 0 ? companyId : (int?)null, locationId > 0 ? locationId : (int?)null, groupId > 0 ? groupId : (int?)null);
                var url = $"{_baseUrl}/api/WindowsUserDetails/allUser{query}";

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
                if (!IsTopLevelAdmin() && companyId > 0 && !IsAuthorized(companyId))
                    return Json(new List<object>());

                var query = BuildScopedQuery(companyId > 0 ? companyId : (int?)null);
                var response = await GetClient().GetAsync($"{_baseUrl}/api/CompaniesDetails/Groupdata{query}");
                var json = await response.Content.ReadAsStringAsync();
                var groups = JArray.Parse(json);
                if (companyId > 0)
                {
                    groups = new JArray(groups.Where(g =>
                    {
                        var gCompId = g["companyID"]?.Value<int?>()
                                   ?? g["CompanyID"]?.Value<int?>()
                                   ?? g["companyId"]?.Value<int?>()
                                   ?? g["CompanyId"]?.Value<int?>()
                                   ?? 0;
                        return gCompId == companyId;
                    }));
                }
                if (!IsTopLevelAdmin())
                {
                    var (_, userGroupIds, _) = GetUserScope();
                    if (userGroupIds.Any())
                    {
                        groups = new JArray(groups.Where(g =>
                        {
                            var id = g["id"]?.Value<int>() ?? g["Id"]?.Value<int>() ?? 0;
                            return userGroupIds.Contains(id);
                        }));
                    }
                }
                return Content(groups.ToString(Newtonsoft.Json.Formatting.None), "application/json");
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
        [DynamicPermission("ServiceDesk.Edit", "Add Ticket Comment")]
        public async Task<IActionResult> AddComment()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Comments", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
        [DynamicPermission("ServiceDesk.Edit", "Add Ticket Attachment")]
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpGet]
        [AuthFilter]
        public async Task<IActionResult> DownloadAttachment(string path, string name)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return NotFound();
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.Edit", "Start Work On Ticket")]
        public async Task<IActionResult> StartWork()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/StartWork", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.Edit", "Resolve Ticket")]
        public async Task<IActionResult> ResolveTicket()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Tickets/Resolve", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
        [DynamicPermission("ServiceDesk.ManageParts", "Add Inventory Part")]
        public async Task<IActionResult> AddInventoryPart()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var payload = JsonConvert.DeserializeObject<JObject>(body);
                var companyId = payload?["companyId"]?.Value<int?>() ?? payload?["CompanyId"]?.Value<int?>();
                var groupId = payload?["groupId"]?.Value<int?>() ?? payload?["GroupId"]?.Value<int?>();
                var locationId = payload?["locationId"]?.Value<int?>() ?? payload?["LocationId"]?.Value<int?>();
                if (!ValidateInventoryScope(companyId, groupId, locationId))
                {
                    return Forbid();
                }

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/MasterParts", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }


        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageParts", "Update Master Parts Inventory")]
        public async Task<IActionResult> UpdateInventoryPart()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var payload = JsonConvert.DeserializeObject<JObject>(body);
                var companyId = payload?["companyId"]?.Value<int?>() ?? payload?["CompanyId"]?.Value<int?>();
                var groupId = payload?["groupId"]?.Value<int?>() ?? payload?["GroupId"]?.Value<int?>();
                var locationId = payload?["locationId"]?.Value<int?>() ?? payload?["LocationId"]?.Value<int?>();
                if (!ValidateInventoryScope(companyId, groupId, locationId))
                {
                    return Forbid();
                }

                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/UpdateMasterPart", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }


        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.ManageParts", "Delete Master Parts Inventory")]
        public async Task<IActionResult> DeleteInventoryPart(int id)
        {
            try
            {
                var queryResponse = await GetClient().GetAsync($"{_baseUrl}/api/ServiceDesk/MasterParts");
                if (!queryResponse.IsSuccessStatusCode)
                {
                    return StatusCode((int)queryResponse.StatusCode, "Unable to validate inventory scope");
                }

                var raw = await queryResponse.Content.ReadAsStringAsync();
                var parts = JsonConvert.DeserializeObject<JArray>(raw);
                var part = parts?.FirstOrDefault(p =>
                    p?["id"]?.Value<int?>() == id || p?["Id"]?.Value<int?>() == id);
                var companyId = part?["companyId"]?.Value<int?>() ?? part?["CompanyId"]?.Value<int?>();
                var groupId = part?["groupId"]?.Value<int?>() ?? part?["GroupId"]?.Value<int?>();
                var locationId = part?["locationId"]?.Value<int?>() ?? part?["LocationId"]?.Value<int?>();
                if (!ValidateInventoryScope(companyId, groupId, locationId))
                {
                    return Forbid();
                }

                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/DeleteMasterPart?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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
        [DynamicPermission("ServiceDesk.AdminSettings", "View All Categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Categories/All");
                return Content(response, "application/json");
            }
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Create Category")]
        public async Task<IActionResult> CreateCategory()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Categories", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Update Category")]
        public async Task<IActionResult> UpdateCategory()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Categories/Update", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Delete Category")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Categories/Delete?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }


        [HttpGet]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "View All Priorities")]
        public async Task<IActionResult> GetAllPriorities()
        {
            try
            {
                var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Priorities/All");
                return Content(response, "application/json");
            }
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Create Priority")]
        public async Task<IActionResult> CreatePriority()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Priorities", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Update Priority")]
        public async Task<IActionResult> UpdatePriority()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Priorities/Update", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Delete Priority")]
        public async Task<IActionResult> DeletePriority(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Priorities/Delete?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }


        [HttpGet]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "View All Statuses")]
        public async Task<IActionResult> GetAllStatuses()
        {
            try
            {
                var response = await GetClient().GetStringAsync($"{_baseUrl}/api/ServiceDesk/Statuses/All");
                return Content(response, "application/json");
            }
            catch (Exception ex) { return Json(new { error = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Create Status")]
        public async Task<IActionResult> CreateTicketStatus()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Statuses", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Update Status")]
        public async Task<IActionResult> UpdateTicketStatus()
        {
            try
            {
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Statuses/Update", content);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
        }

        [HttpPost]
        [AuthFilter]
        [DynamicPermission("ServiceDesk.AdminSettings", "Delete Status")]
        public async Task<IActionResult> DeleteTicketStatus(int id)
        {
            try
            {
                var response = await GetClient().PostAsync($"{_baseUrl}/api/ServiceDesk/Statuses/Delete?id={id}", null);
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
            }
            catch (Exception ex) { return Json(new { success = false, message = "An internal server error occurred." }); }
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