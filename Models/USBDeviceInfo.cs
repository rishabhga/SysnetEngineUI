namespace ManageEngineWebApp.Models
{
    public class USBDeviceInfo
    {
        public int Id { get; set; }
        public string DeviceName { get; set; }
        public string UserName { get; set; }
        public string DeviceType { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceInstanceId { get; set; }
        public string ConnectedTime { get; set; }
        public string DisconnectedTime { get; set; }
        public string UsageDuration { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
