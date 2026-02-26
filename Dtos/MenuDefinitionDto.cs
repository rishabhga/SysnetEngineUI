namespace ManageEngineWebApp.Dtos
{
    public class MenuDefinitionDto
    {
        public int Id { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public string RouteController { get; set; } = string.Empty;
        public string RouteAction { get; set; } = string.Empty;
        public string MenuIcon { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int? ParentId { get; set; }
        public string RequiredPermissionCode { get; set; } = string.Empty;
        public int ModuleId { get; set; }
    }
}
