namespace ManageEngineWebApp.Models.SwitchMoniterModels
{
    public class SwitchTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MatchKey { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<SwitchTemplateItem> Items { get; set; }
    }
}
