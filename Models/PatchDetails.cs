namespace ManageEngineWebApp.Models
{
    public class PatchDetails
    {
        public int Id { get; set; }
        public string ComputerName { get; set; }
        public string KB { get; set; }

        public string Size { get; set; }
        public string Title { get; set; }

        public string Status { get; set; }
        public string UserCode { get; set; }

        public DateTime DateTime { get; set; }
    }
}
