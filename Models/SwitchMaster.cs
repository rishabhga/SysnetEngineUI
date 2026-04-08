namespace ManageEngineWebApp.Models
{
    public class SwitchMaster
    {

        public int Id { get; set; }

        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public string Community { get; set; }

        public bool IsActive { get; set; }
        public string DeviceType { get; set; }
    }
}
