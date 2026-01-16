namespace ManageEngineWebApp.Models
{
    public class DiskUsage
    {
        public int Id { get; set; }

        public string Drive { get; set; }
        public long TotalSpaceGB { get; set; }
        public long UsedSpaceGB { get; set; }
        public long FreeSpaceGB { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
