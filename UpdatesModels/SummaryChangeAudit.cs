namespace ManageEngineWebApp.UpdatesModels
{
    public class SummaryChangeAudit
    {
        public int Id { get; set; }
        public string FieldName { get; set; }
        public string PreviousValue { get; set; }
        public string ChangedValue { get; set; }
        public string UserCode { get; set; }
        public DateTime ChangeDateTime { get; set; }
    }
}
