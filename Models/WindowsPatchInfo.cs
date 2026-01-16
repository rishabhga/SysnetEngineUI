namespace ManageEngineWebApp.Models
{
    public class WindowsPatchInfo
    {
        public int Id { get; set; }
        public string Caption { get; set; }
        public string HotFixID { get; set; }
        public string InstalledOn { get; set; }
        public string InstalledBy { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
