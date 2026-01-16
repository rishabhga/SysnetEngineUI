namespace ManageEngineWebApp.Models
{
    public class WindowsUserDetailsUpdateName
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int GroupId { get; set; }
        public int LocationId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string DomainName { get; set; }
    }
}
