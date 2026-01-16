namespace ManageEngineWebApp.Models
{
    public class ApplicationSettings
    {
        public int Id {  get; set; }
        public bool InstallNonStoreApps { get; set; }
        public bool InstallAppsOnlyInDeviceMemory { get; set; }
        public bool StoreAppDataOnlyInDeviceMemory { get; set; }
        public bool AutoUpdateStoreApps { get; set; }
        public string UserCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
