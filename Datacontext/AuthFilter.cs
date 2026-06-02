using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ManageEngineWebApp.Datacontext
{
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

            if (string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            var rolesStr = session.GetString("roles") ?? session.GetString("role") ?? "";
            var userRoles = rolesStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList();

            if (!userRoles.Any())
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                return;
            }

            if (!RoleHelper.IsTopLevelAdmin(context.HttpContext))
            {
                if (AllowedHierarchyLevel > -1)
                {
                    int userLevel = RoleHelper.GetHierarchyLevel(context.HttpContext);
                    if (userLevel > AllowedHierarchyLevel)
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(AllowedRoles))
                {
                    var allowedRolesList = AllowedRoles
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList();

                    if (!allowedRolesList.Any(r => userRoles.Contains(r)))
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(RequiredPermission))
                {
                    if (!RoleHelper.HasPermission(context.HttpContext, RequiredPermission))
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth",
                            new { requiredPermission = RequiredPermission });
                        return;
                    }
                }
            }

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
                    var allowedCompanyIds = sessionCompanyId
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToHashSet();

                    if (!allowedCompanyIds.Contains(routeCompanyId.Value.ToString()))
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
                        return;
                    }
                }
            }

            base.OnActionExecuting(context);
        }

        private static int? GetCompanyIdFromRoute(ActionExecutingContext context)
        {
            var parameters = new[] { "id", "ComId", "companyId" };
            foreach (var param in parameters)
            {
                if (context.ActionArguments.TryGetValue(param, out var value) &&
                    value != null &&
                    int.TryParse(value.ToString(), out int companyId))
                {
                    return companyId;
                }
            }
            return null;
        }
    }
}