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
        public int DeviceId { get; set; }
        public long PowerOnHours { get; set; }
        public int Temperature { get; set; }
        public int Wear { get; set; }
        public long ReadErrorsTotal { get; set; }
        public long WriteErrorsTotal { get; set; }
        public long ReadErrorsCorrected { get; set; }
        public string UserCode { get; set; }
        public string FirmwareVersion { get; set; }
        public string InterfaceType { get; set; }
        public string HealthStatus { get; set; }
        public bool PredictFailure { get; set; }
        public double FreeSpaceGB { get; set; }
        public double UsedSpaceGB { get; set; }
        public DateTime DateTime { get; set; }
    }
}