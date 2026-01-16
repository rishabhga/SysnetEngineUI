namespace ManageEngineWebApp.Models
{
    public class VideoDeviceInfo
    {
        public int Id { get; set; }
        public string AdapterName { get; set; }
        public string AdapterCompatibility { get; set; }
        public string AdapterRAM { get; set; }
        public string HorizontalResolution { get; set; }
        public string VerticalResolution { get; set; }
        public string DriverVersion { get; set; }
        public string InstallDate { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
