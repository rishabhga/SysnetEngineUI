namespace ManageEngineWebApp.Models
{
    public class BIOSDetails
    {

        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Version { get; set; }
        public string SMBiosVersion { get; set; }
        public string ReleaseDate { get; set; }
        public int YearOfInstallation { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
