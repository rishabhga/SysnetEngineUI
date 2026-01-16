namespace ManageEngineWebApp.Models
{
    public class PatchDetail
    {
        public int Id { get; set; }
        public string PatchId { get; set; }           // Unique identifier (like KB number)
        public string Bulletin { get; set; }          // Security bulletin reference (e.g., MS24-001)
        public string PatchName { get; set; }         // Title of the update
        public string PatchDescription { get; set; }  // Detailed description
        public string Vendor { get; set; }  // Vendor name
        public string PatchType { get; set; }         // e.g., Security, Feature, Cumulative
        public string Severity { get; set; }          // Critical / Important / Moderate /
        public string Source { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
