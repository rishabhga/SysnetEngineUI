namespace ManageEngineWebApp.Requests
{
    public class CreateRoleWithPermissionsRequest
    {
        public string? RoleName { get; set; }
        public string? Description { get; set; }
        public bool RequiresCompany { get; set; }
        public bool RequiresGroup { get; set; }
        public bool RequiresDevice { get; set; }
        public bool RequiresLocation { get; set; }
        public List<string>? Permissions { get; set; }
        public List<int>? MenuIds { get; set; }
    }
}
