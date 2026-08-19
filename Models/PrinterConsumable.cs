using System;

namespace ManageEngineWebApp.Models
{
    public class PrinterConsumable
    {
        public int Id { get; set; }
        public int PrinterInformationId { get; set; }
        public string PrinterIPAddress { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int CurrentLevel { get; set; }
        public int MaximumLevel { get; set; }
        public int Percentage { get; set; }
        public string Status { get; set; }
        public DateTime ScanDate { get; set; }
    }
}