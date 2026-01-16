namespace ManageEngineWebApp.Models
{
    public class AntivirusDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProtectionStatus { get; set; }
        public string LicenseStatus { get; set; }
        public string Version { get; set; }
        public string Manufacturer { get; set; }
        public string InstallationPath { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
