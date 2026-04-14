namespace ManageEngineWebApp.Models
{
    public class PatchDetail
    {
        public int Id { get; set; }
        public string PatchId { get; set; }          
        public string Bulletin { get; set; }        
        public string PatchName { get; set; }      
        public string PatchDescription { get; set; }  
        public string Vendor { get; set; }  
        public string PatchType { get; set; }       
        public string Severity { get; set; }        
        public string Source { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
