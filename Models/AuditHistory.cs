namespace ManageEngineWebApp.Models
{
    public class AuditHistory
    {
        public string DeviceName { get; set; }
        public string UserName { get; set; }
        public string DeviceType { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceInstanceId { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime DisconnectedTime { get; set; }
        public string UserCode { get; set; }
        //public TimeSpan UsageDuration { get; set; }
    }
}
