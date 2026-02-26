using ManageEngineWebApp.Datacontext;

namespace ManageEngineWebApp.Filters
{
    public class CompanyDataFilter : AuthFilter
    {
        public CompanyDataFilter()
        {
            AllowedHierarchyLevel = 5;
            VerifyCompanyAccess = true;
        }
    }
}
