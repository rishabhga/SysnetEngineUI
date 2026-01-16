namespace ManageEngineWebApp.Models
{
    public class RestrictionOnNetwork
    {
        public int Id { get; set; }
        public bool InternetSharing { get; set; }
        public bool VPN { get; set; }
        public bool WiFi { get; set; }
        public bool AllowWiFiConfiguration { get; set; }
        public bool AutoConnectWiFiSense { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
