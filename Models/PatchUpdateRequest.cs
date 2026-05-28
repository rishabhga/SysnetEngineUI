namespace ManageEngineWebApp.Models
{
    public class PatchUpdateRequest
    {
        public int SoftwareId { get; set; }
        public string SoftwareName { get; set; }  // Software ka naam (e.g., Google Chrome)
        public string Version { get; set; }
        public string DownloadUrl { get; set; }   // Patch ka download link
        public string Description { get; set; }
        public string Command { get; set; }
    }
}
