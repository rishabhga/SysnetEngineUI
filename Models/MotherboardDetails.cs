namespace ManageEngineWebApp.Models
{
    public class MotherboardDetails
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string Product { get; set; }
        public string Version { get; set; }
        public string PrimaryBusType { get; set; }
        public string SecondaryBusType { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
