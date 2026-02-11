using System;

namespace ManageEngineWebApp.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class DynamicPermissionAttribute : Attribute
    {
        public string? Code { get; }
        public string? Name { get; }
        public string? Description { get; }
        public string? Module { get; }
        public string? ActionType { get; set; } // View, Create, Edit, Delete, Action, etc.

        public DynamicPermissionAttribute(string? code = null, string? name = null, string? description = null, string? module = null)
        {
            Code = code;
            Name = name;
            Description = description;
            Module = module;
        }
    }
}
