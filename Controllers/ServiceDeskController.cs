using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class ServiceDeskController : Controller
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7225/api";

        // Mock data for demo - Will be replaced with actual database
        private static List<ServiceTicketDto> _tickets = new List<ServiceTicketDto>
        {
            new ServiceTicketDto { Id = 1, TicketNo = "INC000001", Subject = "Printer not working", Category = "Hardware", Priority = "High", Status = "Open", RequesterName = "John Smith", DomainName = "DESKTOP-ABC123", LocationName = "Main Office - Floor 1", CreatedDate = DateTime.Now.AddDays(-2) },
            new ServiceTicketDto { Id = 2, TicketNo = "INC000002", Subject = "Cannot access email", Category = "Email", Priority = "Critical", Status = "Assigned", RequesterName = "Alice Johnson", DomainName = "LAPTOP-XYZ789", LocationName = "Branch Office", AssignedToName = "Mike Engineer", CreatedDate = DateTime.Now.AddDays(-1) },
            new ServiceTicketDto { Id = 3, TicketNo = "INC000003", Subject = "Software installation request", Category = "Software", Priority = "Medium", Status = "InProgress", RequesterName = "Bob Wilson", DomainName = "DESKTOP-DEF456", LocationName = "Main Office - Floor 2", AssignedToName = "Sarah Tech", CreatedDate = DateTime.Now.AddHours(-5) },
            new ServiceTicketDto { Id = 4, TicketNo = "INC000004", Subject = "Network slow performance", Category = "Network", Priority = "High", Status = "OnHold", RequesterName = "Carol Davis", DomainName = "DESKTOP-GHI789", LocationName = "Remote Office", AssignedToName = "Mike Engineer", CreatedDate = DateTime.Now.AddDays(-3) },
            new ServiceTicketDto { Id = 5, TicketNo = "INC000005", Subject = "Password reset request", Category = "Account", Priority = "Low", Status = "Resolved", RequesterName = "David Brown", DomainName = "LAPTOP-JKL012", LocationName = "Main Office - Floor 1", AssignedToName = "Admin", CreatedDate = DateTime.Now.AddDays(-5), Resolution = "Password reset completed" },
            new ServiceTicketDto { Id = 6, TicketNo = "INC000006", Subject = "Monitor display issues", Category = "Hardware", Priority = "Medium", Status = "Closed", RequesterName = "Eva Martinez", DomainName = "DESKTOP-MNO345", LocationName = "Branch Office", AssignedToName = "Sarah Tech", CreatedDate = DateTime.Now.AddDays(-7) },
        };

        private static List<string> _engineers = new List<string> { "Mike Engineer", "Sarah Tech", "Admin", "John Support", "Lisa IT" };
        private static List<TicketStatusLogDto> _statusLogs = new List<TicketStatusLogDto>();

        public ServiceDeskController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IActionResult Index(string status = null, string priority = null, int? locationId = null)
        {
            var tickets = _tickets.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                tickets = tickets.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                tickets = tickets.Where(t => t.Priority == priority);
            }

            if (locationId.HasValue)
            {
                tickets = tickets.Where(t => t.LocationId == locationId.Value);
            }
            var userRole = HttpContext.Session.GetString("role");
            if (userRole == "LocationAdmin" || userRole == "Engineer")
            {
            }

            var viewModel = new ServiceDeskViewModel
            {
                Tickets = tickets.OrderByDescending(t => t.CreatedDate).ToList(),
                TotalCount = _tickets.Count,
                OpenCount = _tickets.Count(t => t.Status == "Open"),
                AssignedCount = _tickets.Count(t => t.Status == "Assigned"),
                InProgressCount = _tickets.Count(t => t.Status == "InProgress"),
                OnHoldCount = _tickets.Count(t => t.Status == "OnHold"),
                ResolvedCount = _tickets.Count(t => t.Status == "Resolved"),
                ClosedCount = _tickets.Count(t => t.Status == "Closed")
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetTicketDetails(int id)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Ticket not found" });
            }
            return Json(ticket);
        }

        [HttpPost]
        public IActionResult CreateTicket([FromBody] CreateTicketRequest request)
        {
            try
            {
                var username = HttpContext.Session.GetString("username") ?? "Guest";
                
                var newTicket = new ServiceTicketDto
                {
                    Id = _tickets.Count + 1,
                    TicketNo = $"INC{(_tickets.Count + 1).ToString().PadLeft(6, '0')}",
                    Subject = request.Subject,
                    Description = request.Description,
                    Category = request.Category,
                    Priority = request.Priority,
                    Status = "Open",
                    RequesterName = username,
                    DomainName = request.DomainName,
                    LocationId = request.LocationId,
                    LocationName = "Location " + request.LocationId, // Get from DB in real app
                    CreatedDate = DateTime.Now,
                    Source = "Web"
                };

                _tickets.Add(newTicket);

                return Json(new { success = true, message = "Ticket created successfully", ticketNo = newTicket.TicketNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AssignTicket([FromBody] AssignTicketRequest request)
        {
            try
            {
                var ticket = _tickets.FirstOrDefault(t => t.Id == request.TicketId);
                if (ticket == null)
                {
                    return Json(new { success = false, message = "Ticket not found" });
                }

                ticket.AssignedToName = request.EngineerId; // In real app, get engineer name from ID
                ticket.AssignedToId = request.EngineerId;
                ticket.AssignedDate = DateTime.Now;
                ticket.Status = "Assigned";

                return Json(new { success = true, message = $"Ticket assigned to {request.EngineerId}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                var ticket = _tickets.FirstOrDefault(t => t.Id == request.TicketId);
                if (ticket == null)
                {
                    return Json(new { success = false, message = "Ticket not found" });
                }

                var oldStatus = ticket.Status;
                ticket.Status = request.Status;
                ticket.LastUpdatedDate = DateTime.Now;
                ticket.LastUpdatedBy = HttpContext.Session.GetString("username");

                if (request.Status == "Resolved")
                {
                    ticket.ResolvedDate = DateTime.Now;
                    ticket.Resolution = request.Resolution;
                }
                else if (request.Status == "Closed")
                {
                    ticket.ClosedDate = DateTime.Now;
                    ticket.ClosedByName = HttpContext.Session.GetString("username");
                }

                // Log history
                _statusLogs.Add(new TicketStatusLogDto {
                    TicketId = ticket.Id,
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    ChangedByName = HttpContext.Session.GetString("username") ?? "System",
                    ChangedDate = DateTime.Now,
                    Remarks = request.Resolution ?? "Status changed"
                });

                // Notify EXE
                SendCommandToExe(ticket.DomainName, "UpdateStatus", $"Ticket {ticket.TicketNo} is now {request.Status}");

                return Json(new { success = true, message = $"Status updated from {oldStatus} to {request.Status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ApproveTicket([FromBody] int ticketId)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null) return Json(new { success = false, message = "Ticket not found" });

            var oldStatus = ticket.Status;
            ticket.Status = "InProgress";
            ticket.LastUpdatedDate = DateTime.Now;
            
            _statusLogs.Add(new TicketStatusLogDto {
                TicketId = ticket.Id,
                OldStatus = oldStatus,
                NewStatus = "InProgress",
                ChangedByName = HttpContext.Session.GetString("username"),
                ChangedDate = DateTime.Now,
                Remarks = "Ticket Approved by User"
            });

            SendCommandToExe(ticket.DomainName, "ShowMessage", "Your ticket has been approved and work has started.");

            return Json(new { success = true, message = "Ticket approved" });
        }

        [HttpPost]
        public IActionResult RejectTicket([FromBody] int ticketId)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket == null) return Json(new { success = false, message = "Ticket not found" });

            var oldStatus = ticket.Status;
            ticket.Status = "Rejected";
            ticket.LastUpdatedDate = DateTime.Now;

             _statusLogs.Add(new TicketStatusLogDto {
                TicketId = ticket.Id,
                OldStatus = oldStatus,
                NewStatus = "Rejected",
                ChangedByName = HttpContext.Session.GetString("username"),
                ChangedDate = DateTime.Now,
                Remarks = "Ticket Rejected by User"
            });

            SendCommandToExe(ticket.DomainName, "ShowMessage", "Your ticket has been rejected.");

            return Json(new { success = true, message = "Ticket rejected" });
        }

        private void SendCommandToExe(string domainName, string type, string data)
        {
            // In real app, save to ExeCommands table in DB
            // _context.ExeCommands.Add(new ExeCommand { ... });
        }

        [HttpGet]
        public IActionResult GetEngineers(int? locationId = null)
        {
            // In real app, filter engineers by location
            var engineers = _engineers.Select(e => new { id = e, name = e }).ToList();
            return Json(engineers);
        }

        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = new[]
            {
                new { value = "Hardware", label = "Hardware" },
                new { value = "Software", label = "Software" },
                new { value = "Network", label = "Network" },
                new { value = "Email", label = "Email" },
                new { value = "Account", label = "Account/Access" },
                new { value = "Other", label = "Other" }
            };
            return Json(categories);
        }

        // ============ Work Logs ============
        [HttpGet]
        public IActionResult GetWorkLogs(int ticketId)
        {
            // Mock data
            var workLogs = new List<object>
            {
                new { id = 1, ticketId, technicianName = "Mike Engineer", workDescription = "Initial analysis completed", workDate = DateTime.Now.AddDays(-1), timeSpentMinutes = 30, callType = "Remote" },
                new { id = 2, ticketId, technicianName = "Mike Engineer", workDescription = "Replaced faulty component", workDate = DateTime.Now, timeSpentMinutes = 60, callType = "OnSite" }
            };
            return Json(workLogs);
        }

        [HttpGet]
        public IActionResult GetStatusHistory(int ticketId)
        {
            var logs = _statusLogs.Where(l => l.TicketId == ticketId).OrderByDescending(l => l.ChangedDate).ToList();
            return Json(logs);
        }

        [HttpPost]
        public IActionResult AddWorkLog([FromBody] AddWorkLogRequest request)
        {
            try
            {
                // In real app, save to database
                return Json(new { success = true, message = "Work log added successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============ Parts ============
        [HttpGet]
        public IActionResult GetParts(int ticketId)
        {
            // Mock data
            var parts = new List<object>
            {
                new { id = 1, ticketId, partName = "RAM Module 8GB", partNumber = "RAM-DDR4-8GB", quantity = 1, unitCost = 45.00, totalCost = 45.00 }
            };
            return Json(parts);
        }

        [HttpPost]
        public IActionResult AddPart([FromBody] AddPartRequest request)
        {
            try
            {
                // In real app, save to database
                return Json(new { success = true, message = "Part added successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============ Comments ============
        [HttpGet]
        public IActionResult GetComments(int ticketId)
        {
            // Mock data
            var comments = new List<object>
            {
                new { id = 1, ticketId, commentText = "User reported issue", authorName = "System", isInternal = false, createdDate = DateTime.Now.AddDays(-2) },
                new { id = 2, ticketId, commentText = "Checked remotely, need on-site visit", authorName = "Mike Engineer", isInternal = true, createdDate = DateTime.Now.AddDays(-1) }
            };
            return Json(comments);
        }

        [HttpPost]
        public IActionResult AddComment([FromBody] AddCommentRequest request)
        {
            try
            {
                // In real app, save to database
                return Json(new { success = true, message = "Comment added successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============ Dashboard Stats ============
        [HttpGet]
        public IActionResult GetDashboardStats()
        {
            var stats = new
            {
                totalTickets = _tickets.Count,
                openTickets = _tickets.Count(t => t.Status == "Open"),
                assignedTickets = _tickets.Count(t => t.Status == "Assigned"),
                inProgressTickets = _tickets.Count(t => t.Status == "InProgress"),
                resolvedToday = _tickets.Count(t => t.Status == "Resolved" && t.ResolvedDate?.Date == DateTime.Today),
                avgResolutionTime = "4.5 hours",
                slaBreached = 2,
                slaMet = _tickets.Count - 2
            };
            return Json(stats);
        }
    }

    public class ServiceDeskViewModel
    {
        public List<ServiceTicketDto> Tickets { get; set; } = new List<ServiceTicketDto>();
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int AssignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int OnHoldCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
    }

    public class ServiceTicketDto
    {
        public int Id { get; set; }
        public string TicketNo { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string RequesterName { get; set; }
        public string DomainName { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public string AssignedToId { get; set; }
        public string AssignedToName { get; set; }
        public DateTime? AssignedDate { get; set; }
        public string Resolution { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ClosedByName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string LastUpdatedBy { get; set; }
        public string Source { get; set; }
    }

    public class CreateTicketRequest
    {
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string DomainName { get; set; }
        public int? LocationId { get; set; }
    }

    public class AssignTicketRequest
    {
        public int TicketId { get; set; }
        public string EngineerId { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateStatusRequest
    {
        public int TicketId { get; set; }
        public string Status { get; set; }
        public string Resolution { get; set; }
    }

    public class AddWorkLogRequest
    {
        public int TicketId { get; set; }
        public string WorkDescription { get; set; }
        public int TimeSpentMinutes { get; set; }
        public string CallType { get; set; }
        public bool IsBillable { get; set; }
    }

    public class AddPartRequest
    {
        public int TicketId { get; set; }
        public string PartName { get; set; }
        public string PartNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string Notes { get; set; }
    }

    public class AddCommentRequest
    {
        public int TicketId { get; set; }
        public string CommentText { get; set; }
        public bool IsInternal { get; set; }
    }

    public class TicketStatusLogDto
    {
        public int TicketId { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string ChangedByName { get; set; }
        public DateTime ChangedDate { get; set; }
        public string Remarks { get; set; }
    }
}
