using System;

namespace ManageEngineWebApp.Models
{
    public class MasterPart
    {
        public int Id { get; set; }
        public string PartName { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public decimal UnitCost { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
}
