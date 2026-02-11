using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace ManageEngineWebApp.Datacontext
{
    public class AuthFilter : ActionFilterAttribute
    {
        public string AllowedRoles { get; set; }
        public string? RequiredPermission { get; set; }
        public bool VerifyCompanyAccess { get; set; } = false;
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

            // SuperAdmin bypasses all checks
            if (role == "SuperAdmin")
            {
                // skip permission checks
            }
            else if (!string.IsNullOrEmpty(AllowedRoles))
            {
                var allowedRolesList = AllowedRoles.Split(',').Select(r => r.Trim()).ToList();

                // If the role is NOT in the allowed list AND user has no dynamic permissions, block
                if (!allowedRolesList.Contains(role))
                {
                    // Check if user has dynamic permissions that grant access (custom roles)
                    var userPermissions = session.GetString("permissions") ?? "";
                    if (string.IsNullOrEmpty(userPermissions))
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                        return;
                    }
                    // User has a custom role with dynamic permissions — let DynamicAuthorizationFilter handle it
                }
            }

            // For non-SuperAdmin with explicit RequiredPermission, verify it
            if (role != "SuperAdmin" && !string.IsNullOrEmpty(RequiredPermission))
            {
                if (!RoleHelper.HasPermission(context.HttpContext, RequiredPermission))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { requiredPermission = RequiredPermission });
                    return;
                }
            }

            if (VerifyCompanyAccess && role != "SuperAdmin")
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
    public class SuperAdminOnlyFilter : AuthFilter
    {
        public SuperAdminOnlyFilter()
        {
            AllowedRoles = "SuperAdmin";
        }
    }
    public class CompanyDataFilter : AuthFilter
    {
        public CompanyDataFilter()
        {
            AllowedRoles = "SuperAdmin,CompanyAdmin";
            VerifyCompanyAccess = true;
        }
    }


    public class CompanyUserFilter : AuthFilter
    {
        public CompanyUserFilter()
        {
            AllowedRoles = "SuperAdmin,CompanyAdmin,CompanyUser";
            VerifyCompanyAccess = true;
        }
    }
}
