using ManageEngineWebApp.Dtos;
using Newtonsoft.Json;
using System.Text;
namespace ManageEngineWebApp.Datacontext
{
    public static class RoleHelper
    {
        private static string _apiBaseUrl = string.Empty;
        private static IHttpClientFactory? _httpClientFactory;

        public static void Configure(IConfiguration configuration, IHttpClientFactory? httpClientFactory = null)
        {
            var baseUrl = configuration["ApiSettings:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _apiBaseUrl = $"{baseUrl}/api/Auth";
            }
            _httpClientFactory = httpClientFactory;
        }

        private static string ApiBaseUrl => _apiBaseUrl;

        private static HttpClient CreateClient()
        {
            if (_httpClientFactory == null)
                throw new InvalidOperationException(
                    "RoleHelper.Configure() must be called with a valid IHttpClientFactory before making API calls.");

            return _httpClientFactory.CreateClient("ManageEngineApi");
        }

        public static async Task<(UserRoleDto? Result, string? Error)> GetUserRoleFromApiAsync(string username)
        {
            try
            {
                using var client = CreateClient();
                var response = await client.GetAsync($"{ApiBaseUrl}/user/roles/{username}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<UserRoleDto>(json);
                    return (result, null);
                }
                var errorMsg = await response.Content.ReadAsStringAsync();
                return (null, $"Backend Error ({response.StatusCode}): {errorMsg}");
            }
            catch (Exception ex)
            {
                return (null, $"Connection Error: {ex.Message}");
            }
        }


        public static bool IsTopLevelAdmin(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var level = context?.Session.GetString("hierarchyLevel");
            return level == "0";
        }


        public static bool IsCompanyScopedRole(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var flag = context?.Session.GetString("requiresCompany");
            return flag == "true";
        }

        public static bool IsDeviceScopedRole(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var flag = context?.Session.GetString("requiresDevice");
            return flag == "true";
        }
        public static bool IsSuperAdmin(Microsoft.AspNetCore.Http.HttpContext context) => IsTopLevelAdmin(context);
        public static bool IsCompanyAdmin(Microsoft.AspNetCore.Http.HttpContext context) => IsCompanyScopedRole(context);
        public static bool IsCompanyUser(Microsoft.AspNetCore.Http.HttpContext context) => IsDeviceScopedRole(context);

        public static int GetHierarchyLevel(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var level = context?.Session.GetString("hierarchyLevel");
            if (int.TryParse(level, out int h)) return h;
            return 999;
        }

        public static bool HasPermission(Microsoft.AspNetCore.Http.HttpContext context, string permissionCode)
        {
            if (IsTopLevelAdmin(context)) return true; 

            if (string.IsNullOrEmpty(permissionCode)) return true;

            var permissions = context?.Session.GetString("permissions");
            if (string.IsNullOrEmpty(permissions)) return false;

            if (permissions == "*") return true;

            var permList = permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            if (permList.Any(p => p.Equals(permissionCode, StringComparison.OrdinalIgnoreCase)))
                return true;

            var parts = permissionCode.Split('.');
            if (parts.Length == 2)
            {
                string module = parts[0];
                string action = parts[1];
                string viewPerm = $"{module}.View";
                if (permList.Any(p => p.Equals(viewPerm, StringComparison.OrdinalIgnoreCase)))
                {
                    var writeActions = new[] { "Edit", "Delete", "Create", "Update", "Remove", "Add", "Assign" };
                    if (!writeActions.Any(w => action.Contains(w, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }

            return false;
        }
        public static List<int> GetCompanyIds(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var idStr = context?.Session.GetString("companyId");
            if (string.IsNullOrEmpty(idStr)) return new List<int>();
            return idStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
        }

        public static int? GetCompanyId(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var ids = GetCompanyIds(context);
            if (ids.Any()) return ids.First();
            return null;
        }

        public static List<int> GetGroupIds(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var idStr = context?.Session.GetString("groupId");
            if (string.IsNullOrEmpty(idStr)) return new List<int>();
            return idStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
        }

        public static int? GetGroupId(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var ids = GetGroupIds(context);
            if (ids.Any()) return ids.First();
            return null;
        }

        public static List<int> GetLocationIds(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var idStr = context?.Session.GetString("locationId");
            if (string.IsNullOrEmpty(idStr)) return new List<int>();
            return idStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int id) ? id : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
        }

        public static int? GetLocationId(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var ids = GetLocationIds(context);
            if (ids.Any()) return ids.First();
            return null;
        }

        public static bool ValidateScope(
            Microsoft.AspNetCore.Http.HttpContext context,
            int? requestedCompanyId,
            int? requestedGroupId = null,
            int? requestedLocationId = null)
        {
            if (IsTopLevelAdmin(context)) return true;
            
            var userCompanyIds = GetCompanyIds(context);
            if (userCompanyIds.Any() && requestedCompanyId.HasValue && !userCompanyIds.Contains(requestedCompanyId.Value))
                return false;
                
            var userGroupIds = GetGroupIds(context);
            if (userGroupIds.Any() && requestedGroupId.HasValue && !userGroupIds.Contains(requestedGroupId.Value))
                return false;
                
            var userLocationIds = GetLocationIds(context);
            if (userLocationIds.Any() && requestedLocationId.HasValue && !userLocationIds.Contains(requestedLocationId.Value))
                return false;

            return true;
        }

        public static void SetSessionFromRoleData(Microsoft.AspNetCore.Http.HttpContext context, UserRoleDto roleData, string primaryRole)
        {
            context.Session.SetString("username", roleData.Username ?? "");
            context.Session.SetString("role", primaryRole);
            context.Session.SetString("hierarchyLevel", roleData.HierarchyLevel.ToString());
            context.Session.SetString("requiresCompany", roleData.RequiresCompany.ToString().ToLower());
            context.Session.SetString("requiresGroup", roleData.RequiresGroup.ToString().ToLower());
            context.Session.SetString("requiresLocation", roleData.RequiresLocation.ToString().ToLower());
            context.Session.SetString("requiresDevice", roleData.RequiresDevice.ToString().ToLower());
            context.Session.SetString("isSystemRole", roleData.IsSystemRole.ToString().ToLower());

            if (!string.IsNullOrEmpty(roleData.StartPage))
                context.Session.SetString("startPage", roleData.StartPage);

            // Always clear old scope values first to prevent stale data
            context.Session.Remove("companyId");
            context.Session.Remove("groupId");
            context.Session.Remove("locationId");

            var companyIds = new List<int>();
            if (roleData.CompanyId.HasValue) companyIds.Add(roleData.CompanyId.Value);
            if (roleData.Mappings != null) companyIds.AddRange(roleData.Mappings.Where(m => m.CompanyId.HasValue).Select(m => m.CompanyId.Value));
            if (companyIds.Any()) context.Session.SetString("companyId", string.Join(",", companyIds.Distinct()));
                
            var groupIds = new List<int>();
            if (roleData.GroupId.HasValue) groupIds.Add(roleData.GroupId.Value);
            if (roleData.Mappings != null) groupIds.AddRange(roleData.Mappings.Where(m => m.GroupId.HasValue).Select(m => m.GroupId.Value));
            if (groupIds.Any()) context.Session.SetString("groupId", string.Join(",", groupIds.Distinct()));

            var locationIds = new List<int>();
            if (roleData.LocationId.HasValue) locationIds.Add(roleData.LocationId.Value);
            if (roleData.Mappings != null) locationIds.AddRange(roleData.Mappings.Where(m => m.LocationId.HasValue).Select(m => m.LocationId.Value));
            if (locationIds.Any()) context.Session.SetString("locationId", string.Join(",", locationIds.Distinct()));

            var permString = roleData.Permissions != null && roleData.Permissions.Any()
                ? string.Join(",", roleData.Permissions)
                : "";
            context.Session.SetString("permissions", permString);

            var menuIdString = roleData.AllowedMenuIds != null && roleData.AllowedMenuIds.Any()
                ? string.Join(",", roleData.AllowedMenuIds)
                : "";
            context.Session.SetString("allowedMenuIds", menuIdString);

            if (!string.IsNullOrEmpty(roleData.StartPage))
                context.Session.SetString("startPage", roleData.StartPage);
        }

        public static async Task RefreshSessionPermissionsAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            var username = context.Session.GetString("username");
            if (string.IsNullOrEmpty(username)) return;

            var roleResponse = await GetUserRoleFromApiAsync(username);
            if (roleResponse.Result != null)
            {
                var roleData = roleResponse.Result;
                string primaryRole = "No Role";
                if (roleData.Roles != null && roleData.Roles.Any())
                {
                    primaryRole = roleData.Roles.First(); 
                }

                SetSessionFromRoleData(context, roleData, primaryRole);

                ClearMenuCache(context);
            }
        }

        public static async Task<bool> AssignRoleAsync(string? username, string? role, int? companyId, string? domainName = null, int? groupId = null, int? locationId = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role)) return false;
            try
            {
                using var client = CreateClient();

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
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RemoveRoleAsync(string username)
        {
            try
            {
                using var client = CreateClient();

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
                using var client = CreateClient();
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

        public static async Task<List<SystemRoleDto>> GetAllSystemRolesAsync()
        {
            try
            {
                using var client = CreateClient();
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
                    // No hardcoded role names — IsSystem defaults to false for all via fallback
                    return roleNames.Select(r => new SystemRoleDto { Name = r, IsSystem = false }).ToList();
                }
                return new List<SystemRoleDto>();
            }
            catch
            {
                return new List<SystemRoleDto>();
            }
        }

        public static async Task<(bool Success, string Message)> CreateRoleAsync(string? roleName, string? description, 
            bool requiresCompany, bool requiresDevice, bool requiresLocation, bool requiresGroup = false)
        {
            if (string.IsNullOrEmpty(roleName)) return (false, "Role name is required");
            try
            {
                using var client = CreateClient();

                var payload = new
                {
                    RoleName = roleName,
                    Description = description,
                    RequiresCompany = requiresCompany,
                    RequiresGroup = requiresGroup,
                    RequiresDevice = requiresDevice,
                    RequiresLocation = requiresLocation,
                    HierarchyLevel = (roleName == "SuperAdmin") ? 0 : 10,
                    DisplayName = roleName
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
            catch (Exception)
            {
                return (false, "Error occurred while creating role");
            }
        }

        public static async Task<bool> DeleteRoleAsync(string? roleName)
        {
            if (string.IsNullOrEmpty(roleName)) return false;
            try
            {
                using var client = CreateClient();
                var response = await client.PostAsync($"{ApiBaseUrl}/role/delete/{roleName}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<List<MenuDefinitionDto>> GetDynamicMenusAsync(Microsoft.AspNetCore.Http.HttpContext? context = null)
        {
            List<MenuDefinitionDto> allMenus = new List<MenuDefinitionDto>();

            if (context != null)
            {
                var cached = context.Session.GetString("cachedMenus");
                if (!string.IsNullOrEmpty(cached))
                {
                    try
                    {
                        allMenus = JsonConvert.DeserializeObject<List<MenuDefinitionDto>>(cached) ?? new List<MenuDefinitionDto>();
                    }
                    catch {  }
                }
            }

            if (!allMenus.Any())
            {
                try
                {
                    using var client = CreateClient();
                    var baseUrl = ApiBaseUrl.Replace("/Auth", "/Permission");
                    var response = await client.GetAsync($"{baseUrl}/Menus");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        allMenus = JsonConvert.DeserializeObject<List<MenuDefinitionDto>>(json) ?? new List<MenuDefinitionDto>();
                        
                        if (context != null)
                        {
                            context.Session.SetString("cachedMenus", json);
                        }
                    }
                }
                catch
                {
                    return new List<MenuDefinitionDto>();
                }
            }

            if (context != null)
            {
                allMenus = allMenus.Where(m => string.IsNullOrEmpty(m.RequiredPermissionCode) || HasPermission(context, m.RequiredPermissionCode)).ToList();

                if (!IsTopLevelAdmin(context))
                {
                    var allowedIdStr = context.Session.GetString("allowedMenuIds");
                    if (allowedIdStr == "-1")
                    {
                        return allMenus;
                    }
                    else if (!string.IsNullOrEmpty(allowedIdStr))
                    {
                        var allowedIds = allowedIdStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => int.TryParse(id, out int result) ? result : -1)
                            .Where(id => id != -1)
                            .ToList();

                        return allMenus.Where(m => allowedIds.Contains(m.Id)).ToList();
                    }
                    else
                    {
                        return allMenus.Where(m => string.IsNullOrEmpty(m.RequiredPermissionCode)).ToList();
                    }
                }
            }

            return allMenus;
        }

        public static void ClearMenuCache(Microsoft.AspNetCore.Http.HttpContext context)
        {
            context.Session.Remove("cachedMenus");
        }
    }
}
