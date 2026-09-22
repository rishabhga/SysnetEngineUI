namespace ManageEngineWebApp.Models.SwitchMoniterModels
{
    public class SwitchTemplateItem
    {
        public int Id { get; set; }
        public int SwitchTemplateId { get; set; }
        public string ItemKey { get; set; }
        public string Oid { get; set; }
        public string Category { get; set; }
        public bool IsTable { get; set; }
        public string SnmpVersion { get; set; } = "Ver2";
    }
}
