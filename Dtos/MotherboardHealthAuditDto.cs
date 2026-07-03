using ManageEngineWebApp.Models;

namespace ManageEngineWebApp.Dtos
{
    public class MotherboardHealthAuditDto
    {
        public int Id { get; set; }
        public string UserCode { get; set; }
        public DateTime AuditDate { get; set; }
        public float MotherboardTemperature { get; set; }
        public float CpuTemperature { get; set; }
        public float Voltage12V { get; set; }
        public float Voltage5V { get; set; }
        public float Voltage3V3 { get; set; }
        public float FanRPM { get; set; }
        public int WheaErrors { get; set; }
        public int HealthScore { get; set; }
        public string Status { get; set; }
        public List<string> Issues { get; set; }
    }
}
