namespace ManageEngineWebApp.Models
{
    public class DeviceSummary
    {
        public int Id { get; set; }
        public string DeviceManufacturer { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceType { get; set; }
        public string Processor { get; set; }
        public string Memory { get; set; }
        public string SerialNumber { get; set; }
        public string ProcessorArchitecture { get; set; }
        public string AssetTag { get; set; }
        public string UDID { get; set; }
        public string EASDeviceIdentifier { get; set; }
        public string BatteryLevel { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
