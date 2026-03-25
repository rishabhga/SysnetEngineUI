using Newtonsoft.Json;

namespace ManageEngineWebApp.Dtos
{
    public class RoleMappingDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("roleName")]
        public string? RoleName { get; set; }
        [JsonProperty("scopeType")]
        public string? ScopeType { get; set; }
        [JsonProperty("scopeName")]
        public string? ScopeName { get; set; }
        [JsonProperty("scopeId")]
        public int? ScopeId { get; set; }
        [JsonProperty("companyId")]
        public int? CompanyId { get; set; }
        [JsonProperty("groupId")]
        public int? GroupId { get; set; }
        [JsonProperty("locationId")]
        public int? LocationId { get; set; }
        [JsonProperty("companyName")]
        public string? CompanyName { get; set; }
        [JsonProperty("groupName")]
        public string? GroupName { get; set; }
        [JsonProperty("locationName")]
        public string? LocationName { get; set; }
    }
}
