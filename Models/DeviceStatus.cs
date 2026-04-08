namespace ManageEngineWebApp.Models
{
    public class DeviceStatus
    {

        public int Id { get; set; }

        public string DeviceIp { get; set; }
        public string DeviceName { get; set; }
        public string UpTime { get; set; }
        public string Status { get; set; }

        public DateTime CreatedOn { get; set; }

        public int SwitchMasterId { get; set; }
        public SwitchMaster SwitchMaster { get; set; }

        public List<PortStatus> Ports { get; set; }
    }
}
