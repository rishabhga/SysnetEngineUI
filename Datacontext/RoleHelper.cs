using Newtonsoft.Json;
using System.Text;
namespace ManageEngineWebApp.Datacontext
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
    }

    public class RoleMappingDto
    {
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
    }
    public static class RoleHelper
    {
        private static string _apiBaseUrl = "https://localhost:7225/api/Auth";
        public static void Configure(IConfiguration configuration)
        {
            var baseUrl = configuration["ApiSettings:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _apiBaseUrl = $"{baseUrl}/api/Auth";
            }
        }
        private static string ApiBaseUrl => _apiBaseUrl;

        public static async Task<UserRoleDto> GetUserRoleFromApiAsync(string username)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);
                var response = await client.GetAsync($"{ApiBaseUrl}/user/roles/{username}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<UserRoleDto>(json);
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"GetUserRoleFromApiAsync Failed. Status: {response.StatusCode}, Content: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserRoleFromApiAsync Exception: {ex.Message}");
                return null;
            }
        }
        public static bool IsSuperAdmin(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var role = context?.Session.GetString("role");
            return role == "SuperAdmin";
        }
        public static bool IsCompanyAdmin(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var role = context?.Session.GetString("role");
            return role == "CompanyAdmin";
        }
        public static bool HasPermission(Microsoft.AspNetCore.Http.HttpContext context, string permissionCode)
        {
            var role = context?.Session.GetString("role");
            if (role == "SuperAdmin") return true; 

            var permissions = context?.Session.GetString("permissions");
            if (string.IsNullOrEmpty(permissions)) return false;

            return permissions.Split(',').Any(p => p.Trim() == permissionCode);
        }
        public static int? GetCompanyId(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var companyIdStr = context?.Session.GetString("companyId");
            if (int.TryParse(companyIdStr, out int companyId))
            {
                return companyId;
            }
            return null;
        }
        public static async Task<bool> AssignRoleAsync(string username, string role, int? companyId, string domainName = null, int? groupId = null, int? locationId = null)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);

                var payload = new
                {
                    Username = username,
                    Role = role,
                    CompanyId = companyId,
                    GroupId = groupId,
                    LocationId = locationId,
                    DomainName = domainName
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{ApiBaseUrl}/role/assign", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return apiResponse?.success == true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static async Task<bool> RemoveRoleAsync(string username)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);

                var payload = new
                {
                    Username = username,
                    Role = (string)null,
                    CompanyId = (int?)null
                };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{ApiBaseUrl}/role/remove", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(result);
                    return apiResponse?.success == true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<List<UserRoleDto>> GetAllRolesAsync()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);
                var response = await client.GetAsync($"{ApiBaseUrl}/roles");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<UserRoleDto>>(json);
                }
                return new List<UserRoleDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: GetAllRolesAsync Error: {ex.Message}");
                return new List<UserRoleDto>();
            }
        }
        public static bool IsCompanyUser(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var role = context?.Session.GetString("role");
            return role == "CompanyUser";
        }

        public static async Task<List<SystemRoleDto>> GetAllSystemRolesAsync()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);
                var response = await client.GetAsync($"{ApiBaseUrl}/roles/system");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<SystemRoleDto>>(json) ?? new List<SystemRoleDto>();
                }
                // Fallback to basic roles list from identity
                response = await client.GetAsync($"{ApiBaseUrl}/roles/list");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var roleNames = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                    return roleNames.Select(r => new SystemRoleDto { Name = r, IsSystem = r == "SuperAdmin" || r == "CompanyAdmin" || r == "CompanyUser" }).ToList();
                }
                return new List<SystemRoleDto>();
            }
            catch
            {
                return new List<SystemRoleDto>();
            }
        }

        public static async Task<(bool Success, string Message)> CreateRoleAsync(string roleName, string description, 
            bool requiresCompany, bool requiresDevice, bool requiresLocation)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);

                var payload = new
                {
                    RoleName = roleName,
                    Description = description,
                    RequiresCompany = requiresCompany,
                    RequiresDevice = requiresDevice,
                    RequiresLocation = requiresLocation
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{ApiBaseUrl}/role/create", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Role created successfully");
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                try 
                {
                   var errorObj = JsonConvert.DeserializeObject<dynamic>(errorContent);
                   return (false, (string)errorObj?.message ?? "Failed to create role");
                }
                catch 
                {
                   return (false, "Failed to create role: " + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                return (false, "Error: " + ex.Message);
            }
        }

        public static async Task<bool> DeleteRoleAsync(string roleName)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
                };
                using var client = new HttpClient(handler);
                var response = await client.DeleteAsync($"{ApiBaseUrl}/role/delete/{roleName}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class SystemRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
        public bool RequiresCompany { get; set; }
        public bool RequiresDevice { get; set; }
        public bool RequiresLocation { get; set; }
    }
}
