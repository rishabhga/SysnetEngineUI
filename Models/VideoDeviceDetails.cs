namespace ManageEngineWebApp.Models
{
    public class VideoDeviceDetails
    {
        public string Name { get; set; }
        public string AdapterCompatibility { get; set; }
        public long AdapterRAM { get; set; }
        public int HorizontalResolution { get; set; }
        public int VerticalResolution { get; set; }
        public string DriverVersion { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
    }
}
