using Newtonsoft.Json;

namespace ManageEngineWebApp.Dtos
{
    public class SystemRoleDto
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("isSystem")]
        [System.Text.Json.Serialization.JsonPropertyName("isSystem")]
        public bool IsSystem { get; set; }

        [JsonProperty("userCount")]
        [System.Text.Json.Serialization.JsonPropertyName("userCount")]
        public int UserCount { get; set; }

        [JsonProperty("requiresCompany")]
        [System.Text.Json.Serialization.JsonPropertyName("requiresCompany")]
        public bool RequiresCompany { get; set; }

        [JsonProperty("requiresGroup")]
        [System.Text.Json.Serialization.JsonPropertyName("requiresGroup")]
        public bool RequiresGroup { get; set; }

        [JsonProperty("requiresDevice")]
        [System.Text.Json.Serialization.JsonPropertyName("requiresDevice")]
        public bool RequiresDevice { get; set; }

        [JsonProperty("requiresLocation")]
        [System.Text.Json.Serialization.JsonPropertyName("requiresLocation")]
        public bool RequiresLocation { get; set; }

        [JsonProperty("hierarchyLevel")]
        [System.Text.Json.Serialization.JsonPropertyName("hierarchyLevel")]
        public int HierarchyLevel { get; set; }

        [JsonProperty("startPage")]
        [System.Text.Json.Serialization.JsonPropertyName("startPage")]
        public string StartPage { get; set; } = string.Empty;
    }
}
