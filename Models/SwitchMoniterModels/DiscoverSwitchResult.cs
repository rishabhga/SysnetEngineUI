namespace ManageEngineWebApp.Models.SwitchMoniterModels
{
    public class DiscoverSwitchResult
    {
        public string SysDescr { get; set; }
        public List<DiscoveredOidItem> WorkingOids { get; set; } = new();
    }
}
