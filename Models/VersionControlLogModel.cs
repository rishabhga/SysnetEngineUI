namespace ManageEngineWebApp.Models
{
    public class VersionControlLogModel
    {
        public int Id { get; set; }
        public string PreviousVersion { get; set; } = string.Empty;
        public string NewVersion { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
