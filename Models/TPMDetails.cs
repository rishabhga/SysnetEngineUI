namespace ManageEngineSoftware.Models
{
    public class TPMDetails
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerVersion { get; set; }
        public string SpecificationVersion { get; set; }
        public bool Activated { get; set; }
        public bool Enabled { get; set; }
        public bool Owned { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
