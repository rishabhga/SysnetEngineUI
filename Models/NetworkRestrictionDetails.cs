namespace ManageEngineWebApp.Models
{
    public class NetworkRestrictionDetails
    {
        public bool InternetSharing { get; set; }
        public bool VPN { get; set; }
        public bool AllowWiFi { get; set; }
        public bool AutoConnectWiFiSense { get; set; }
        public bool AllowWiFiConfiguration { get; set; }
        public string UserCode { get; set; }

        public DateTime DateTime { get; set; }
    }
}
