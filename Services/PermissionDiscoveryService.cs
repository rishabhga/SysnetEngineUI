using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ManageEngineWebApp.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace ManageEngineWebApp.Services
{
    public class PermissionDiscoveryService
    {
        public class DiscoveredPermission
        {
            public string PermissionCode { get; set; }
            public string PermissionName { get; set; }
            public string Module { get; set; }
            public string Description { get; set; }
            public string ActionType { get; set; }
            public int SortOrder { get; set; }
            public string Controller { get; set; }
            public string Action { get; set; }
        }

        public List<DiscoveredPermission> DiscoverPermissions()
        {
            var permissions = new List<DiscoveredPermission>();
            var assembly = Assembly.GetExecutingAssembly();
            
            var controllers = assembly.GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type) && !type.IsAbstract);

            foreach (var controller in controllers)
            {
                var controllerName = controller.Name.Replace("Controller", "");
                
                var classAttr = controller.GetCustomAttribute<DynamicPermissionAttribute>();
                var moduleName = classAttr?.Module ?? controllerName;

                var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName && (typeof(IActionResult).IsAssignableFrom(m.ReturnType) || typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType)));

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<DynamicPermissionAttribute>();
                    var actionName = method.Name;

                    if (method.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
                        continue;

                    var code = attr?.Code ?? $"{controllerName}.{actionName}";
                    var name = attr?.Name ?? SplitCamelCase(actionName);
                    var desc = attr?.Description ?? $"{name} in {moduleName}";
                    var actionType = attr?.ActionType ?? InferActionType(actionName);
                    
                    permissions.Add(new DiscoveredPermission
                    {
                        PermissionCode = code,
                        PermissionName = name,
                        Module = moduleName,
                        Description = desc,
                        ActionType = actionType,
                        SortOrder = 0,
                        Controller = controllerName,
                        Action = actionName
                    });
                }
            }
            return permissions.DistinctBy(p => p.PermissionCode).ToList();
        }

        private string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1").Trim();
        }

        private string InferActionType(string actionName)
        {
            if (actionName.StartsWith("Create") || actionName.StartsWith("Add") || actionName.StartsWith("Save")) return "Create";
            if (actionName.StartsWith("Edit") || actionName.StartsWith("Update")) return "Edit";
            if (actionName.StartsWith("Delete") || actionName.StartsWith("Remove")) return "Delete";
            if (actionName.StartsWith("View") || actionName.StartsWith("Index") || actionName.StartsWith("Get") || actionName.StartsWith("Detail")) return "View";
            if (actionName.Contains("Approve")) return "Approve";
            if (actionName.Contains("Assign")) return "Assign";
            return "Action";
        }
    }
}
