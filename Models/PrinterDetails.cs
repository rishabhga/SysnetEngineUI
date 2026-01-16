namespace ManageEngineWebApp.Models
{
    public class PrinterDetails
    {
        public int Id { get; set; } 
        public string PrinterName { get; set; }
        public string ConnectedUser { get; set; }
        public bool DefaultPrinter { get; set; }
        public string PortName { get; set; }
        public string Manufacturer { get; set; }
        public string DeviceStatus { get; set; }
        public string ServerName { get; set; }
        public string PrinterType { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
