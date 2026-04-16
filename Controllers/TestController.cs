using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter(AllowedHierarchyLevel = 0)]
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
