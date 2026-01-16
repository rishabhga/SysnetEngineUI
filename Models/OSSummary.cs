namespace ManageEngineWebApp.Models
{
    public class OSSummary
    {

        public int Id { get; set; }
        public string OperatingSystem { get; set; }
        public string OSVersion { get; set; }
        public string RegisteredTo { get; set; }
        public string ProductID { get; set; }
        public string LicenseType { get; set; }
        public string SystemDrive { get; set; }
        public string OSCDKey { get; set; }
        public string OSServicePack { get; set; }
        public string OSBuildNumber { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
