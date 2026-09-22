using ManageEngineWebApp.Models.SwitchMoniterModels;

namespace ManageEngineWebApp.Models
{
    public class SwitchMaster
    {
        public int Id { get; set; }

        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public string Community { get; set; }

        public bool IsActive { get; set; }
        public string DeviceType { get; set; }
        public string PollingMode { get; set; }
        public int? LocationId { get; set; }
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public SwitchTemplate? Template { get; set; }
    }
}