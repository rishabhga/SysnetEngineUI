namespace ManageEngineWebApp.Dtos
{
    public class WindowsUserDetailsDto
    {
        public int Id { get; set; }
        public int PreviousId { get; set; }
        public string UserName { get; set; }
        public string PreviousUserName { get; set; }

        public string DomainName { get; set; }
        public string PreviousDomainName { get; set; }
        
        public string SID { get; set; }
        public string PreviousSID { get; set; }
        public string AccountType { get; set; }
        public string PreviousAccountType { get; set; }
        public string FullName { get; set; }
        public string PreviousFullName { get; set; }
        public string Status { get; set; }
        public string PreviousStatus { get; set; }
        public string UserCode { get; set; }
        public string PreviousUserCode { get; set; }
    }
}
