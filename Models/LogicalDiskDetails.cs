namespace ManageEngineWebApp.Models
{
    public class LogicalDiskDetails
    {
        public int Id { get; set; }
        public string DriveLetter { get; set; }
        public string FileSystem { get; set; }
        public double FreeSpace { get; set; } // GB
        public double TotalCapacity { get; set; } // GB
        public double UsedSpace { get; set; } // GB
        public double UsagePercentage { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
