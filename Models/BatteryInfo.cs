namespace ManageEngineWebApp.Models
{
    public class BatteryInfo
    {
        public int Id { get; set; }

        public string Manufacturer { get; set; }
        public string BatteryName { get; set; }
        public string SerialNumber { get; set; }
        public string Description { get; set; }
        public string Chemistry { get; set; }

        public string Status { get; set; }
        public string SystemType { get; set; }

        public int BatteryPercentage { get; set; }
        public bool IsCharging { get; set; }

        public int? EstimatedRunTime { get; set; }

        public long? DesignCapacity { get; set; }
        public long? FullChargeCapacity { get; set; }
        public long? RemainingCapacity { get; set; }

        public decimal? BatteryHealthPercent { get; set; }
        public decimal? WearLevelPercent { get; set; }

        public int? CycleCount { get; set; }

        public string UserCode { get; set; }

        public DateTime ScanDate { get; set; }

        // Keeping for backward compatibility if needed, or map correctly
        public string BatteryLevel { get; set; }
        public DateTime DateTime { get; set; }
    }
}
