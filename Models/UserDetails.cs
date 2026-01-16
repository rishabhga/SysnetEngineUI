namespace ManageEngineWebApp.Models
{
    public class UserDetails
    {
        public string UserName { get; set; }
        public string domainName { get; set; }
        public string WindowName { get; set; }
        public string IpAddress { get; set; }
        public string PrimaryOwner { get; set; }
        public string OsLicenseStatus { get; set; }
        public string LastBootTime { get; set; }
        public DateTime DateTime { get; set; }
    }
}
