using System;

namespace ManageEngineWebApp.Models
{
    public class HelpdeskTicket
    {
        public int Id { get; set; }
        public string? TicketNo { get; set; }
        public string? ClientId { get; set; }
        public int? ProblemId { get; set; }
        public string? ProblemName { get; set; }
        public string? Subject { get; set; } 
        public string? Remark { get; set; }
        public string? Status { get; set; } 
        public string? Priority { get; set; }
        public string? Category { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; } 
        public int? CompanyId { get; set; }
        public int? LocationId { get; set; }
        public int? GroupId { get; set; }
        public string? CompanyName { get; set; }
        public string? LocationName { get; set; }
        public string? GroupName { get; set; }
        public int? SlaDuration { get; set; }
        public string? SlaUnit { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool? IsSLABreached { get; set; }
        public string? RequesterName { get; set; } 
        public string? RequesterEmail { get; set; } 
    }
}
