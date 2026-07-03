using ManageEngineWebApp.Models;

namespace ManageEngineWebApp.Dtos
{

    public class MotherboardAuditSnapshotDto
    {
        public MotherboardHealthAuditDto Health { get; set; }
        public CpuHardwareDetails Cpu { get; set; }
    }

}
