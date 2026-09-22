namespace ManageEngineWebApp.Models.SwitchMoniterModels
{
    public class DiscoveredOidItem
    {
        public string ItemKey { get; set; }
        public string Oid { get; set; }
        public string SnmpVersion { get; set; }
        public int SampleCount { get; set; }
        public string SampleValue { get; set; }
    }
}
