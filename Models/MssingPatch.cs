namespace ManageEngineWebApp.Models
{
    public class MssingPatch
    {
        public int Id { get; set; }
        public string PatchId { get; set; }           
        public string Bulletin { get; set; }
        public string Name { get; set; }          
        public string Version { get; set; }         
        public string Available { get; set; }         
        public string Source { get; set; }  
        public string Vendor { get; set; } = "Microsoft"; 
        public string PatchType { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
