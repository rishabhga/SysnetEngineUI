using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Authorization;
using ManageEngineWebApp.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Linq;
using System.Threading.Tasks;

namespace ManageEngineWebApp.Filters
{
    public class DynamicAuthorizationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            {
                await next();
                return;
            }

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            
            if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
            {
                await next();
                return;
            }

            // Skip permission checks for Auth and Home controllers (Login, Logout, AccessDenied, etc.)
            var skipControllers = new[] { "Auth", "Home" };
            if (skipControllers.Contains(controller, StringComparer.OrdinalIgnoreCase))
            {
                await next();
                return;
            }
            
            // Default permission code
            string permissionCode = $"{controller}.{action}";

            // Check for attribute override
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                var attr = descriptor.MethodInfo.GetCustomAttributes(typeof(DynamicPermissionAttribute), false).FirstOrDefault() as DynamicPermissionAttribute;
                if (attr?.Code != null)
                {
                    permissionCode = attr.Code;
                }
            }

            // Check if user is authenticated
            var username = context.HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                // User not logged in
                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new { success = false, message = "Please login to continue." }) { StatusCode = 401 };
                }
                else
                {
                    context.Result = new RedirectToActionResult("Login", "Auth", null);
                }
                return; // Always return when not authenticated
            }

            // Check if user is SuperAdmin or has permission
            if (!RoleHelper.HasPermission(context.HttpContext, permissionCode))
            {
                var sessionPerms = context.HttpContext.Session.GetString("permissions") ?? "(null)";
                var userRole = context.HttpContext.Session.GetString("role") ?? "(no role)";
                System.Diagnostics.Debug.WriteLine($"[RBAC DENIED] User role={userRole}, Required={permissionCode}, HasPerms={sessionPerms.Length > 0}, Perms={sessionPerms}");

                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new { success = false, message = "Access Denied" });
                }
                else
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { requiredPermission = permissionCode });
                }
                return;
            }

            await next();
        }

        private bool IsAjaxRequest(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
