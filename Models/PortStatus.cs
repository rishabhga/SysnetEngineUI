namespace ManageEngineWebApp.Models
{
    public class PortStatus
    {
        public int Id { get; set; }

        public int DeviceStatusId { get; set; }
        public DeviceStatus DeviceStatus { get; set; }

        public string PortName { get; set; }
        public string PortState { get; set; }

        public long RX { get; set; }
        public long TX { get; set; }
    }
}
