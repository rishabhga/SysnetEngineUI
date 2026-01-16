namespace ManageEngineWebApp.Models
{
    public class MicrosoftStoreAppDetailsClass
    {

        public int Id { get; set; }
        public string Name { get; set; }  // Package Name
        public string PackageFullName { get; set; }  // Full Package Name
        public string DisplayName { get; set; }  // Friendly Name
        public string Version { get; set; }  // Version
        public string Manufacturer { get; set; }  // Manufacturer
        public string OSCompatibility { get; set; }  // OS Compatibility
        public string InstalledAccount { get; set; }  // User Account
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
