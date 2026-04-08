namespace ManageEngineWebApp.Models
{
    public class AgentPollHistory
    {

        public int Id { get; set; }
        public string AgentIp { get; set; }
        public string Status { get; set; }
        public string UpTime { get; set; }

        public string Cpu { get; set; }
        public string Ram { get; set; }
        public string Disk { get; set; }

        public DateTime PollTime { get; set; }
    }
}
