using ManageEngineWebApp.Datacontext;

namespace ManageEngineWebApp.Filters
{
    public class CompanyUserFilter : AuthFilter
    {
        public CompanyUserFilter()
        {
            AllowedHierarchyLevel = 10;
            VerifyCompanyAccess = true;
        }
    }
}
