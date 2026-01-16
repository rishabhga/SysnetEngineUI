namespace ManageEngineWebApp.Models
{
    public class InstalledApplication
    {
        public int Id { get; set; }
        public string SoftwareName { get; set; }
        public string Version { get; set; }
        public string OSCompatibility { get; set; }
        public string Manufacturer { get; set; }
        public string InstalledDate { get; set; }
        public string Usages { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }// This can store usage-related info, e.g., last accessed, if available.
    }
}
