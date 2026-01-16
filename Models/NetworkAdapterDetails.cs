namespace ManageEngineWebApp.Models
{
    public class NetworkAdapterDetails
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string MACAddress { get; set; }
        public string IPAddress { get; set; }
        public string DNSHostName { get; set; }
        public string DNSServerSearchOrder { get; set; }
        public bool DHCPEnabled { get; set; }
        public string DHCPLeaseObtained { get; set; }
        public string DHCPLeaseExpires { get; set; }
        public string DHCPServer { get; set; }
        public string DefaultIPGateway { get; set; }
        public string IPSubnet { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string ConnectionStatus { get; set; }
        public string Primary { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
