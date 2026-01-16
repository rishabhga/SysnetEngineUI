namespace ManageEngineWebApp.Models
{
    public class BluetoothDetails
    {
        public int Id { get; set; }
        public bool Bluetooth { get; set; }
        public bool Bluetoothdiscovery { get; set; }
        public bool Bluetoothprepairing { get; set; }
        public bool Bluetoothservicesadvertising { get; set; }
        public DateTime DateTime { get; set; }
        public string UserCode { get; set; }
    }
}
