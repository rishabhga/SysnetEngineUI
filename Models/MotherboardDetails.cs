namespace ManageEngineWebApp.Models
{
    public class MotherboardDetails
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string Product { get; set; }
        public string Version { get; set; }
        public string PrimaryBusType { get; set; }
        public string SecondaryBusType { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }

    public class MotherboardHealth
    {
        public int Id { get; set; }
        public float MotherboardTemperature { get; set; }
        public float CpuTemperature { get; set; }
        public float Voltage12V { get; set; }
        public float Voltage5V { get; set; }
        public float Voltage3V3 { get; set; }
        public float FanRPM { get; set; }
        public int WheaErrors { get; set; }
        public int HealthScore { get; set; }
        public DateTime AuditDate { get; set; }
        public string UserCode { get; set; }
    }

    public class CpuHardwareDetails
    {
        public int Id { get; set; }
        public string UserCode { get; set; }
        public float CpuTotalLoad { get; set; }
        public float CpuCore1Load { get; set; }
        public float CpuCore2Load { get; set; }
        public float CpuCore3Load { get; set; }
        public float CpuCore4Load { get; set; }
        public float CpuPackageTemp { get; set; }
        public float CpuCore1Temp { get; set; }
        public float CpuCore2Temp { get; set; }
        public float CpuCore3Temp { get; set; }
        public float CpuCore4Temp { get; set; }
        public float CoreAverageTemp { get; set; }
        public float CoreMaxTemp { get; set; }
        public float CpuPackagePower { get; set; }
        public float CpuCorePower { get; set; }
        public float BusSpeed { get; set; }
        public float CpuCore1Clock { get; set; }
        public float CpuCore2Clock { get; set; }
        public float CpuCore3Clock { get; set; }
        public float CpuCore4Clock { get; set; }
        public DateTime AuditDate { get; set; }
    }
}
