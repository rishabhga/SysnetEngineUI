using System;

namespace ManageEngineWebApp.Models
{
    public class TemplateItem
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string OID { get; set; } = string.Empty;
        public string DataType { get; set; } = "String";
        public string? Unit { get; set; }
        public string? Group { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
