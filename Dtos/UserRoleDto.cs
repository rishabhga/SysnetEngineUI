using ManageEngineWebApp.Datacontext;
using Newtonsoft.Json;

namespace ManageEngineWebApp.Dtos
{
    public class UserRoleDto
    {
        [JsonProperty("username")]
        public string? Username { get; set; }
        [JsonProperty("roles")]
        public List<string>? Roles { get; set; }
        [JsonProperty("mappings")]
        public List<RoleMappingDto>? Mappings { get; set; }
        [JsonProperty("companyId")]
        public int? CompanyId { get; set; }
        [JsonProperty("groupId")]
        public int? GroupId { get; set; }
        [JsonProperty("locationId")]
        public int? LocationId { get; set; }
        [JsonProperty("startPage")]
        public string? StartPage { get; set; }
        [JsonProperty("permissions")]
        public List<string>? Permissions { get; set; }
        [JsonProperty("hierarchyLevel")]
        public int HierarchyLevel { get; set; } = 999;
        [JsonProperty("requiresCompany")]
        public bool RequiresCompany { get; set; }
        [JsonProperty("requiresGroup")]
        public bool RequiresGroup { get; set; }
        [JsonProperty("requiresLocation")]
        public bool RequiresLocation { get; set; }
        [JsonProperty("requiresDevice")]
        public bool RequiresDevice { get; set; }
        [JsonProperty("isSystemRole")]
        public bool IsSystemRole { get; set; }
        [JsonProperty("allowedMenuIds")]
        public List<int>? AllowedMenuIds { get; set; }
    }
}
