namespace ManageEngineWebApp.Models
{
    public class USBControllerInfo
    {
        public int Id { get; set; }
        public string HardwareName { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceStatus { get; set; }
        public string DeviceStatusInfo { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
