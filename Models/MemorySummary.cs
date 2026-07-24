namespace ManageEngineWebApp.Models
{
    public class MemorySummary
    {
        public int Id { get; set; }
        public double InstalledMemoryGB { get; set; }

        public double MaximumSupportedMemoryGB { get; set; }

        public int TotalSlots { get; set; }

        public int UsedSlots { get; set; }

        public double FreeMemoryGB { get; set; }

        public double UsedMemoryGB { get; set; }

        public double UsagePercent { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }

        public List<PhysicalMemoryInfo> MemoryModules { get; set; }
        
        public int? HealthScore { get; set; }
        public string HealthLevel { get; set; }
        public string UsageLevel { get; set; }
    }
    public class PhysicalMemoryInfo
    {
        public int Id { get; set; }
        public string DeviceLocator { get; set; }
        //public string BankLabel { get; set; }

        public double CapacityGB { get; set; }

        public string Manufacturer { get; set; }
        public string PartNumber { get; set; }
        public string SerialNumber { get; set; }

        public int SpeedMHz { get; set; }
        public int ConfiguredClockSpeedMHz { get; set; }

        public int DataWidth { get; set; }
        public int TotalWidth { get; set; }

        public string FormFactor { get; set; }
        public string MemoryType { get; set; }

        public int ConfiguredVoltage { get; set; }
        public int MinVoltage { get; set; }
        public int MaxVoltage { get; set; }

        public string SMBIOSMemoryType { get; set; }

        public string Status { get; set; }

        public string Tag { get; set; }

        public string InterleavePosition { get; set; }

        public string TypeDetail { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }

}
