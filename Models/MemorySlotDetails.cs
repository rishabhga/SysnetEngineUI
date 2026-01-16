namespace ManageEngineWebApp.Models
{
    public class MemorySlotDetails
    {
        public int Id { get; set; }
        public string Slot { get; set; }
        public string MemoryType { get; set; }
        public string AvailableMemory { get; set; }
        public string Speed { get; set; }
        public string BankLabel { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
