using Newtonsoft.Json;
using System.Text;
namespace ManageEngineWebApp.Datacontext
{
    public class UserRoleDto
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public int? CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string DomainName { get; set; }
    }
    public static class RoleHelper
    {
        private static readonly string ApiBaseUrl = "https://172.16.15.15:4431/api/Auth";

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
                return null;
            }
            catch
            {
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
        public static int? GetCompanyId(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var companyIdStr = context?.Session.GetString("companyId");
            if (int.TryParse(companyIdStr, out int companyId))
            {
                return companyId;
            }
            return null;
        }
        public static async Task<bool> AssignRoleAsync(string username, string role, int? companyId, string domainName = null)
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
            catch
            {
                return new List<UserRoleDto>();
            }
        }
        public static bool IsCompanyUser(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var role = context?.Session.GetString("role");
            return role == "CompanyUser";
        }
    }
}
