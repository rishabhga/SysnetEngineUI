namespace ManageEngineWebApp.Models
{
    public class BatteryInfo
    {
        public int Id { get; set; }
        public string UserCode { get; set; }
        public string LoggedInUser { get; set; }
        public string BatteryName { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string Chemistry { get; set; }
        public string Description { get; set; }
        public string SystemType { get; set; }
        public int BatteryPercentage { get; set; }
        public bool IsCharging { get; set; }
        public string LiveBatteryDetails { get; set; }
        public int? EstimatedRunTime { get; set; }
        public int? DesignCapacity { get; set; }
        public int? FullChargeCapacity { get; set; }
        public int? RemainingCapacity { get; set; }
        public decimal? BatteryHealthPercent { get; set; }
        public decimal? WearLevelPercent { get; set; }
        public double? WearRatePerMonth { get; set; }
        public int? EstimatedRemainingMonths { get; set; }
        public int? CycleCount { get; set; }
        public string Status { get; set; }
        public string CapacityHistoryJson { get; set; }
        public string UsageHistoryJson { get; set; }
        public string BatteryUsageJson { get; set; }
        public DateTime ScanDate { get; set; }
    }
}