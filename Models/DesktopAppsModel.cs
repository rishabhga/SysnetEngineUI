namespace ManageEngineWebApp.Models
{
    public class DesktopAppsModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Manufacturer { get; set; }
        public string InstallDate { get; set; }
        public string InstallLocation { get; set; }
        public string OSCompatibility { get; set; }
        public string Usages { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
