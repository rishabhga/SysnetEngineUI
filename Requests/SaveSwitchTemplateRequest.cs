using ManageEngineWebApp.Dtos;

namespace ManageEngineWebApp.Requests
{
    public class SaveSwitchTemplateRequest
    {
        public string Name { get; set; }
        public string MatchKey { get; set; }
        public List<SwitchTemplateItemDto> Items { get; set; }
    }
}
