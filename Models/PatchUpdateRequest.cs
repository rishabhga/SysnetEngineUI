namespace ManageEngineWebApp.Models
{
    public class PatchUpdateRequest
    {
        public string SoftwareName { get; set; }  // Software ka naam (e.g., Google Chrome)
        public string DownloadUrl { get; set; }   // Patch ka download link
    }
}
