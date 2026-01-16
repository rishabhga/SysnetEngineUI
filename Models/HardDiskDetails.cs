namespace ManageEngineWebApp.Models
{
    public class HardDiskDetails
    {
        public int Id {  get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string Description { get; set; }
        public double TotalCapacity { get; set; } // GB
        public string UserCode { get; set; }
       
        public DateTime DateTime { get; set; }
    }
}
