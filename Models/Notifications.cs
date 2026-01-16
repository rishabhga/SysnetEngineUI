namespace ManageEngineWebApp.Models
{
    public class Notifications
    {
        public int Id { get; set; }
        public string MachineId { get; set; }
        public string MSNotificationType { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
