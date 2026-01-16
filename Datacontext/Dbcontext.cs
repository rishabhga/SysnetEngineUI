using ManageEngineWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ManageEngineWebApp.Datacontext
{
    public class Dbcontext :DbContext
    {
        public Dbcontext(DbContextOptions<Dbcontext> option) : base(option)
        {
        
        }

        //public DbSet<WindowsUserDetails> windowsUserDetails { get; set; }

        //public DbSet<WindowsService> windowsServices { get; set; }
        //public DbSet<WindowDrivers> WindowDrivers { get; set; }

        //public DbSet<WindowsGroupDetails> windowsGroupDetails { get; set; }
        //public DbSet<HardDiskDetails> HardDiskDetails { get; set; }

        //public DbSet<MotherboardDetails> MotherboardDetails { get; set; }
        //public DbSet<NetworkAdapterDetails> networkAdapterDetails { get; set; }
        //public DbSet<USBHubDetails> USBHubDetails { get; set; }
        //public DbSet<ProcessorDetails> processorDetails { get; set; }
        ////public DbSet<PhysicalMemoryDetails> physicalMemoryDetails { get; set; }
        //public DbSet<BIOSDetails> bIOSDetails { get; set; }
        //public DbSet<InstalledApplication> installedApplications { get; set; }
        //public DbSet<DeviceRestrictionDetails> deviceRestrictionDetails { get; set; }
        //public DbSet<DeviceSummary> deviceSummaries { get; set; }
        //public DbSet<OSSummary> oSSummaries { get; set; }


        //public DbSet<AntivirusDetails> antivirusDetails { get; set; }
        //public DbSet<SocialSearchSettings> SocialSearchSettings { get; set; }

        //public DbSet<ApplicationSettings> applicationSettings { get; set; }
        ////public DbSet<TPMDetails> TPMDetails { get; set; }
        //public DbSet<CustomComputerDetails> customComputerDetails { get; set; }
        //public DbSet<SecurityPrivacyDetails> securityPrivacyDetails { get; set; }
        //public DbSet<SoundDeviceDetails> soundDeviceDetails { get; set; }
        //public DbSet<Summary> summaries { get; set; }

    }

}
