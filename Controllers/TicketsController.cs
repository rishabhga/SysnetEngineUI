using Microsoft.AspNetCore.Mvc;
using ManageEngineWebApp.Models;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Controllers
{
    public class TicketsController : Controller
    {
        // Mock Data Storage for Demo - In a real app, this would be in the DbContext
        private static List<Ticket> _tickets = new List<Ticket>
        {
            new Ticket { Id = 1, Subject = "Printer not working", Domain = "DESKTOP-OHFQCTC", Status = "Open", Priority = "High", CreatedBy = "User", CreatedDate = DateTime.Now.AddDays(-2), AssignedTo = "John Doe" },
            new Ticket { Id = 2, Subject = "Slow performance", Domain = "DESKTOP-OHFQCTC", Status = "In Progress", Priority = "Medium", CreatedBy = "User", CreatedDate = DateTime.Now.AddDays(-5), AssignedTo = "Admin" },
            new Ticket { Id = 3, Subject = "Software Installation", Domain = "LATITUDE-7480", Status = "Closed", Priority = "Low", CreatedBy = "Alice", CreatedDate = DateTime.Now.AddDays(-10), AssignedTo = "John Doe" }
        };

        private static List<string> _engineers = new List<string> { "John Doe", "Jane Smith", "Admin", "Mike Ross" };

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [DynamicPermission("Tickets.View", "View Tickets")]
        public IActionResult Index(int? companyId, int? locationId)
        {
            var tickets = _tickets.AsQueryable();

            if (companyId.HasValue)
            {
                tickets = tickets.Where(t => t.CompanyId == companyId.Value);
                ViewBag.FilterCompanyId = companyId;
            }

            if (locationId.HasValue)
            {
                tickets = tickets.Where(t => t.LocationId == locationId.Value);
                ViewBag.FilterLocationId = locationId;
            }

            return View(tickets.ToList());
        }

        [DynamicPermission("Tickets.Create", "Create Ticket")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Ticket ticket)
        {
            ticket.Id = _tickets.Count + 1;
            ticket.CreatedDate = DateTime.Now;
            ticket.Status = ticket.Status ?? "Open"; // Handle potential null
            
            // Get current user from session if available
            var username = HttpContext.Session.GetString("username");
            ticket.CreatedBy = username ?? "Guest";

            _tickets.Add(ticket);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult CreateFromComputer(string domain, string subject, string priority, string description, int? companyId = null, int? locationId = null)
        {
            var ticket = new Ticket
            {
                Id = _tickets.Count + 1,
                Domain = domain,
                Subject = subject,
                Description = description,
                Priority = priority,
                Status = "Open",
                CreatedDate = DateTime.Now,
                CreatedBy = HttpContext.Session.GetString("username") ?? "System",
                CompanyId = companyId,
                LocationId = locationId
            };
            _tickets.Add(ticket);
            
            // Return JSON for AJAX calls
            return Json(new { success = true, message = "Ticket raised successfully!" });
        }

        [AllowAnonymous]
        public IActionResult GetTicketsForComputer(string domain)
        {
            var tickets = _tickets.Where(t => t.Domain == domain).OrderByDescending(t => t.CreatedDate).ToList();
            return Json(tickets);
        }

        [HttpPost]
        [DynamicPermission("Tickets.Assign", "Assign Ticket to Engineer")]
        public IActionResult AssignEngineer(int ticketId, string engineerName)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.AssignedTo = engineerName;
                ticket.Status = "Assigned";
                return Json(new { success = true, message = $"Ticket assigned to {engineerName}" });
            }
            return Json(new { success = false, message = "Ticket not found" });
        }

        public IActionResult GetEngineers(int? companyId)
        {
            // In a real app, you would filter engineers by companyId
            // var engineers = _dbContext.Users.Where(u => u.Role == "Engineer" && u.CompanyId == companyId).Select(u => u.Username).ToList();
            return Json(_engineers);
        }
    }

    // Simple Ticket Model
    public class Ticket
    {
        public int Id { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? Domain { get; set; } // The Computer Name
        public string? Status { get; set; } // Open, In Progress, Closed, Assigned
        public string? Priority { get; set; } // High, Medium, Low
        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CompanyId { get; set; }
        public int? LocationId { get; set; }
    }
}
