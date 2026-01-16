namespace ManageEngineWebApp.UpdatesModels
{
    public class WindowsUserDetailsUpdates
    {
        public int Id { get; set; }

        // Latest User Details
        public int? LatestId { get; set; }
        public string? LatestUsername { get; set; }
        public string? LatestDomainName { get; set; }
        public string? LatestSID { get; set; }
        public string? LatestAccountType { get; set; }
        public string? LatestFullName { get; set; }
        public string? LatestStatus { get; set; }
        public string? LatestUserCode { get; set; }

        // Previous User Details
        public int? PreviousId { get; set; }
        public string? PreviousUsername { get; set; }
        public string? PreviousDomainName { get; set; }
        public string? PreviousSID { get; set; }
        public string? PreviousAccountType { get; set; }
        public string? PreviousFullName { get; set; }
        public string? PreviousStatus { get; set; }
        public string? PreviousUserCode { get; set; }

        // Timestamp
        public DateTime ChangeDetectedAt { get; set; }
    }
}
