using System;
using System.Collections.Generic;

namespace ManageEngineWebApp.Models
{
    public class PrinterInformation
    {
        public int Id { get; set; }

        public string IPAddress { get; set; }
        public string HostName { get; set; }
        public string PrinterName { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }

        public string MacAddress { get; set; }
        public string InterfaceName { get; set; }
        public string InterfaceStatus { get; set; }
        public string LinkSpeed { get; set; }

        public bool IsOnline { get; set; }
        public string PrinterStatus { get; set; }
        public string ErrorStatus { get; set; }
        public string WarningStatus { get; set; }

        public bool PaperJam { get; set; }
        public bool PaperOut { get; set; }
        public bool CoverOpen { get; set; }
        public bool TonerLow { get; set; }
        public bool InkLow { get; set; }

        public long TotalPagesPrinted { get; set; }
        public long ColorPagesPrinted { get; set; }
        public long MonoPagesPrinted { get; set; }

        public string Uptime { get; set; }
        public DateTime ScanDate { get; set; }

        public List<PrinterConsumable> Consumables { get; set; } = new List<PrinterConsumable>();
    }
}