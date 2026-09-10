namespace ManageEngineWebApp.Models
{
    public class PrinterMetricResult
    {
        public int TemplateItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string OID { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? Group { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
