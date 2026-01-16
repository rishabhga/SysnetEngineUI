namespace ManageEngineWebApp.Models
{
    public class ProcessorDetails
    {
        public int Id {  get; set; }
        public string ProcessorSpeed { get; set; }  // In MHz
        public string Manufacturer { get; set; }
        public string Stepping { get; set; }
        public string Family { get; set; }
        public int NumberOfCores { get; set; }
        public string SocketDesignation { get; set; }
        public string Voltage { get; set; }
        public string Version { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
