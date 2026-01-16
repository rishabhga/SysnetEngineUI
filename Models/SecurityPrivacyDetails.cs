namespace ManageEngineWebApp.Models
{
    public class SecurityPrivacyDetails
    {
         public int Id { get; set; }
        public bool LocationServices { get; set; }
        public bool IsMicrosoftAccountConnected { get; set; }
        public bool CanAddNonMicrosoftAccounts { get; set; }
        public bool CanResetDevice { get; set; }
        public bool ToastNotificationsEnabled { get; set; }
        public bool FIPSComplianceEnabled { get; set; }
        public bool CanAddProvisioningPackage { get; set; }
        public bool CanRemoveProvisioningPackage { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
