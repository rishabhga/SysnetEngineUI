namespace ManageEngineWebApp.Models
{
    public class WindowDrivers
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class DeviceManagerItem
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string DeviceName { get; set; } = "Unknown Device";
        public string Manufacturer { get; set; }
        public string Status { get; set; }
        public string DeviceId { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
