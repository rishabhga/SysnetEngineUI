namespace ManageEngineWebApp.Models
{
    public class DeviceRestrictionDetails
    {
        public int Id { get; set; }
        public bool IsCameraEnabled { get; set; }
        public bool IsTelemetryEnabled { get; set; }
        public bool CanModifyDateTime { get; set; }
        public bool IsBluetoothEnabled { get; set; }
        public string DeviceDateTime { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }

    }
}
