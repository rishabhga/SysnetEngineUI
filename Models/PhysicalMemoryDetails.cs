namespace ManageEngineWebApp.Models
{
    public class PhysicalMemoryDetails
    {
        public int Id {  get; set; } 
        public string MaximumSupportedRAM { get; set; }  // In GB
        public string Location { get; set; }
        public int SlotsAvailable { get; set; }
        public int SlotsUsed { get; set; }
        public List<MemorySlotDetails> Slots { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
