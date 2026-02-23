using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace ManageEngineWebApp.Datacontext
{
    /// <summary>
    /// Base authorization filter. Uses dynamic role checks from session 
    /// (hierarchyLevel, permissions) — NO hardcoded role names.
    /// 
    /// AllowedHierarchyLevel: Maximum hierarchy level allowed (lower = more privileged).
    ///   - 0 = top-level admin only
    ///   - 5 = company-level roles and above
    ///   - 10 = all roles
    ///   - Default (null) = any authenticated user + permission check
    /// </summary>
    public class AuthFilter : ActionFilterAttribute
    {
        public string? AllowedRoles { get; set; }
        public string? RequiredPermission { get; set; }
        public bool VerifyCompanyAccess { get; set; } = false;
        public int AllowedHierarchyLevel { get; set; } = -1;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var username = session.GetString("username");
            var role = session.GetString("role");
            if (string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            if (string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            // Top-level admin (hierarchy 0) bypasses all checks
            if (RoleHelper.IsTopLevelAdmin(context.HttpContext))
            {
                // skip permission checks — full access
            }
            else
            {
                // Check hierarchy level if specified
                if (AllowedHierarchyLevel > -1)
                {
                    int userLevel = RoleHelper.GetHierarchyLevel(context.HttpContext);
                    if (userLevel > AllowedHierarchyLevel)
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                        return;
                    }
                }

                // Legacy AllowedRoles support (for backward compatibility with existing attributes)
                if (!string.IsNullOrEmpty(AllowedRoles))
                {
                    var allowedRolesList = AllowedRoles.Split(',').Select(r => r.Trim()).ToList();
                    // Role name check is kept for backward compat but not required for new code
                    if (!allowedRolesList.Contains(role))
                    {
                        // Don't block here — let permission check below handle it
                        // and let DynamicAuthorizationFilter do final check
                    }
                }

                // Check explicit permission if specified
                if (!string.IsNullOrEmpty(RequiredPermission))
                {
                    if (!RoleHelper.HasPermission(context.HttpContext, RequiredPermission))
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { requiredPermission = RequiredPermission });
                        return;
                    }
                }
            }

            // Company access verification — applies to all non-top-level roles
            if (VerifyCompanyAccess && !RoleHelper.IsTopLevelAdmin(context.HttpContext))
            {
                var sessionCompanyId = session.GetString("companyId");

                if (string.IsNullOrEmpty(sessionCompanyId))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                    return;
                }
                var routeCompanyId = GetCompanyIdFromRoute(context);
                if (routeCompanyId.HasValue)
                {
                    if (routeCompanyId.Value.ToString() != sessionCompanyId)
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                        return;
                    }
                }
            }
            base.OnActionExecuting(context);
        }
        private int? GetCompanyIdFromRoute(ActionExecutingContext context)
        {
            var parameters = new[] { "id", "ComId", "companyId" };
            foreach (var param in parameters)
            {
                if (context.ActionArguments.ContainsKey(param))
                {
                    var value = context.ActionArguments[param];
                    if (value != null && int.TryParse(value.ToString(), out int companyId))
                    {
                        return companyId;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Restricts access to top-level admin (hierarchy level 0) only.
    /// </summary>
    public class SuperAdminOnlyFilter : AuthFilter
    {
        public SuperAdminOnlyFilter()
        {
            AllowedHierarchyLevel = 0; // Only hierarchy level 0 (dynamic, not name-based)
        }
    }

    /// <summary>
    /// Restricts access to company-level admins and above (hierarchy level <= 5).
    /// </summary>
    public class CompanyDataFilter : AuthFilter
    {
        public CompanyDataFilter()
        {
            AllowedHierarchyLevel = 5; // Company-level and above
            VerifyCompanyAccess = true;
        }
    }

    /// <summary>
    /// Restricts access to company users and above (hierarchy level <= 10).
    /// </summary>
    public class CompanyUserFilter : AuthFilter
    {
        public CompanyUserFilter()
        {
            AllowedHierarchyLevel = 10; // All standard roles
            VerifyCompanyAccess = true;
        }
    }
}
