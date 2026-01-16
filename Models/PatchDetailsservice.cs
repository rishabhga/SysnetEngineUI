namespace ManageEngineWebApp.Models
{
    public class PatchDetailsservice
    {
        public int Id { get; set; }
        public string PatchId { get; set; }
        public string Bulletin { get; set; }
        public string PatchName { get; set; }
        public string PatchDescription { get; set; }
        public string Vendor { get; set; }
        public string PatchType { get; set; }
        public string Severity { get; set; }
        public string CurrentVersion { get; set; }
        public string AvailableVersion { get; set; }
        public string Source { get; set; }
        public DateTime DetectedAt { get; set; }
        public bool IsAvailableInRepo {  get; set; }
        public string UserCode { get; set; }
    }
}
