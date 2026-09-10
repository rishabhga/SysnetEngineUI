using System;

namespace ManageEngineWebApp.Models
{
    public class PrinterConfiguration
    {
        public int Id { get; set; }
        public string PrinterName { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; } = 161;
        public string Community { get; set; } = "public";
        public string SNMPVersion { get; set; } = "V2";
        public bool IsEnabled { get; set; } = true;
        public bool IsOnline { get; set; }
        public DateTime? LastScanTime { get; set; }
        public int PollingIntervalMinutes { get; set; } = 15;
        public string ErrorMessage { get; set; } = "Not scanned yet";

        public int? CompanyId { get; set; }
        public int? GroupId { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }

        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public MonitoringTemplate? Template { get; set; }
    }
}