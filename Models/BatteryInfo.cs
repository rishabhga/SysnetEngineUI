namespace ManageEngineWebApp.Models
{
    public class BatteryInfo
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string BatteryLevel { get; set; }
        public string SystemType { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
