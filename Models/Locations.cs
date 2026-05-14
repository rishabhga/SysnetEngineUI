namespace ManageEngineWebApp.Models
{
    public class Locations
    {
        
        public int Id { get; set; }
        public int CompanyID { get; set; }
        public int GroupsID { get; set; }
        public string LocationName { get; set; }
        public bool IsCritical { get; set; }
        public double? CpuThreshold { get; set; }
        public double? RamThreshold { get; set; }
        public double? DiskThreshold { get; set; }
    }
}
    