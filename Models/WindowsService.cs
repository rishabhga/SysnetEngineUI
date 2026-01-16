namespace ManageEngineWebApp.Models
{
    public class WindowsService
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string StartupType { get; set; }
        public string Description { get; set; }
        public string LogonName { get; set; }
        public string State { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
