using System;
namespace ManageEngineWebApp.Models
{
    public class ProcessorDetails
    {
        public int Id { get; set; }
        public string ProcessorSpeed { get; set; }
        public string Manufacturer { get; set; }
        public string stepping { get; set; }
        public string Family { get; set; }
        public string NumberOfCores { get; set; }
        public string SocketDesignation { get; set; }
        public string Voltage { get; set; }
        public string Version { get; set; }
        public string DeviceStatus { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
