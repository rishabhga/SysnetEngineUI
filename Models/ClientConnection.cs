namespace ManageEngineWebApp.Models
{
    public class ClientConnection
    {
        public int Id { get; set; }
        public string ConnectionId { get; set; }
        public string ClientId { get; set; }
        public string ComputerName { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string UserCode { get; set; }
        public DateTime LastConnectedTime { get; set; }
        public bool IsConnected { get; set; }
    }
}