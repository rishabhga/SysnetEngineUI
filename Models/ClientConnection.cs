namespace ManageEngineWebApp.Models
{
    public class ClientConnection
    {
        public int Id { get; set; }  // Primary Key
        public string ConnectionId { get; set; }  // SignalR Connection ID
        public string ClientId { get; set; }  // Unique Client ID
        public string ComputerName { get; set; }  // Computer Name
        public DateTime ConnectedAt { get; set; }  // Connection Timestamp
    }
}
