
namespace ManageEngineWebApp.Dtos
{
    public class SwitchTemplateItemDto
    {
        public string ItemKey { get; set; }
        public string Oid { get; set; }
        public string Category { get; set; }
        public bool IsTable  { get; set; }
        public string SnmpVersion { get; set; }
    }
}
