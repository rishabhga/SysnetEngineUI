namespace ManageEngineWebApp.Models
{
    public class MonitorInfo
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string MonitorType { get; set; }
        public string ScreenHeight { get; set; }
        public string ScreenWidth { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string SerialNumber { get; set; }
        public string InstalledWeek { get; set; }
        public string InstalledYear { get; set; }
        public string MonitorSize { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
