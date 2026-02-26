namespace ManageEngineWebApp.Requests
{
    public class AssignRoleRequest
    {
        public string? Username { get; set; }
        public string? Role { get; set; }
        public int? CompanyId { get; set; }
        public int? GroupId { get; set; }
        public string? DomainName { get; set; }
        public int? LocationId { get; set; }
    }
}
