namespace ManageEngineWebApp.Models
{
    public class Summary
    {
        public int Id { get; set; }
        public int TotalHardware { get; set; }
        public int TotalSoftware { get; set; }
        public int CommercialSoftware { get; set; }
        public int NonCommercialSoftware { get; set; }
        public int ProhibitedSoftware { get; set; }
        public int MissingPatches { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
