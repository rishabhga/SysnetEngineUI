namespace ManageEngineWebApp.UpdatesModels
{
    public class NetworkAdapterChangeAudit
    {

        public int Id { get; set; }
        public string FieldName { get; set; }
        public string PreviousValue { get; set; }
        public string ChangedValue { get; set; }
        public string UserCode { get; set; }
        public DateTime ChangeDate { get; set; }
    }
}
