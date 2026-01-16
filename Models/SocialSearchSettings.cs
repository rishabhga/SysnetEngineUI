namespace ManageEngineWebApp.Models
{
    public class SocialSearchSettings
    {
        public int Id { get; set; }
        public bool CortanaEnabled { get; set; }
        public bool SyncSettingsEnabled { get; set; }
        public bool SearchLocationEnabled { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
