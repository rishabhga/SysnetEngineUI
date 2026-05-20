namespace ManageEngineWebApp.Models
{
    public class VIPClient
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public bool IsVIP { get; set; }
        public int? CompanyID { get; set; }
        public int? GroupsID { get; set; }
        public int? LocationID { get; set; }
        public string Source { get; set; }
        public double? CpuThreshold { get; set; }
        public double? CpuWarningThreshold { get; set; }
        public double? CpuInfoThreshold { get; set; }

        public double? RamThreshold { get; set; }
        public double? RamWarningThreshold { get; set; }
        public double? RamInfoThreshold { get; set; }

        public double? DiskThreshold { get; set; }
        public double? DiskWarningThreshold { get; set; }
        public double? DiskInfoThreshold { get; set; }
    }
}
