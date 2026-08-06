using System;
using System.Collections.Generic;

namespace ManageEngineWebApp.Models
{
    public class ProcessorInfo
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string ProcessorId { get; set; }
        public string Architecture { get; set; }

        public int Cores { get; set; }
        public int LogicalProcessors { get; set; }

        public string Family { get; set; }
        public string Stepping { get; set; }
        public string Revision { get; set; }
        public string SocketDesignation { get; set; }

        public int MaxClockSpeedMHz { get; set; }
        public int CurrentClockSpeedMHz { get; set; }

        public int L1CacheKB { get; set; }
        public int L2CacheKB { get; set; }
        public int L3CacheKB { get; set; }

        public string Status { get; set; }
        public string Voltage { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; }
        public string DeviceId { get; set; }
        public string UserCode { get; set; }

        public double BaseSpeedGHz { get; set; }
        public double TurboSpeedGHz { get; set; }
        public double BusSpeedMHz { get; set; }
        public double Multiplier { get; set; }

        public double Temperature { get; set; } = 0;
        public double CpuPackageTemperature { get; set; }
        public double Core0Temp { get; set; }
        public double Core1Temp { get; set; }
        public double Core2Temp { get; set; }
        public double Core3Temp { get; set; }

        public double CpuCoreClockMHz { get; set; }
        public double CpuPackagePower { get; set; }
        public double CpuVoltage { get; set; }

        public int AddressWidth { get; set; }
        public int DataWidth { get; set; }
        public int ExtClockMHz { get; set; }

        public string UpgradeMethod { get; set; }
        public string ProcessorType { get; set; }
        public string CpuStatus { get; set; }

        public DateTime DateTime { get; set; }

        public int? HealthScore { get; set; }
        public string HealthLevel { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
    }
}