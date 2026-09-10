using System;
using System.Collections.Generic;

namespace ManageEngineWebApp.Models
{
    public class MonitoringTemplate
    {
        public int Id { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = "Printer";
        public string Vendor { get; set; } = "Generic";
        public string? Model { get; set; }
        public string? SysObjectId { get; set; }
        public string? Description { get; set; }
        public string? ConfigJson { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public List<TemplateItem> Items { get; set; } = new List<TemplateItem>();
    }
}
