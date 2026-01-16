namespace ManageEngineWebApp.Models
{
    public class PointingDeviceInfo
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }
        public string NumberOfButtons { get; set; }
        public string Handedness { get; set; }
        public string DeviceInterface { get; set; }
        public string DeviceStatus { get; set; }

        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
