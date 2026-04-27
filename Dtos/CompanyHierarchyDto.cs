using System.Collections.Generic;

namespace ManageEngineWebApp.Dtos
{
    public class CompanyHierarchyDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public List<GroupHierarchyDto> Groups { get; set; } = new List<GroupHierarchyDto>();
    }

    public class GroupHierarchyDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<LocationHierarchyDto> Locations { get; set; } = new List<LocationHierarchyDto>();
    }

    public class LocationHierarchyDto
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public bool IsCritical { get; set; }
        public List<UserHierarchyDto> Users { get; set; } = new List<UserHierarchyDto>();
    }

    public class UserHierarchyDto
    {
        public string UserName { get; set; }
        public string IpAddress { get; set; }
        public string OsLicenseStatus { get; set; }
        public string DomainName { get; set; }
        public string PrimaryOwner { get; set; }
        public bool IsOnline { get; set; }
    }
}
