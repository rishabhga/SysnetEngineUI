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

            var skipControllers = new[] { "Home", "Installer" };
            if (skipControllers.Contains(controller, StringComparer.OrdinalIgnoreCase))
            {
                await next();
                return;
            }
            var username = context.HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                if (string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase))
                {
                    var publicAuthActions = new[] { "Login", "Logout", "Register", "AccessDenied" };
                    if (publicAuthActions.Contains(action, StringComparer.OrdinalIgnoreCase))
                    {
                        await next();
                        return;
                    }
                }

                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new { success = false, message = "Session expired. Please login again." }) { StatusCode = 401 };
                }
                else
                {
                    if (!(string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase) && string.Equals(action, "Login", StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Result = new RedirectToActionResult("Login", "Auth", null);
                    }
                    else
                    {
                        await next();
                        return;
                    }
                }
                return; 
            }

            string permissionCode = $"{controller}.{action}";

            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                var attr = descriptor.MethodInfo.GetCustomAttributes(typeof(DynamicPermissionAttribute), false).FirstOrDefault() as DynamicPermissionAttribute;
                if (attr?.Code != null)
                {
                    permissionCode = attr.Code;
                }
            }

            if (!RoleHelper.HasPermission(context.HttpContext, permissionCode))
            {


                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new { success = false, message = "Access Denied" }) { StatusCode = 403 };
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
            // jQuery $.ajax sets X-Requested-With, but fetch() API does not
            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return true;
            // Check if caller expects JSON (covers fetch() with Accept header)
            var accept = request.Headers["Accept"].ToString();
            if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;
            // Check if the request body is JSON
            var contentType = request.ContentType;
            if (!string.IsNullOrEmpty(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }
}
