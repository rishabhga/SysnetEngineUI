using ManageEngineWebApp.Datacontext;

namespace ManageEngineWebApp.Filters
{
    public class SuperAdminOnlyFilter : AuthFilter
    {
        public SuperAdminOnlyFilter()
        {
            AllowedHierarchyLevel = 0;
        }
    }
}
