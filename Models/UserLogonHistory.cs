namespace ManageEngineWebApp.Models
{
    public class UserLogonHistory
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string LogonTime { get; set; }
        public string LogoffTime { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
