using Microsoft.AspNetCore.Mvc;

namespace ManageEngineWebApp.Controllers
{
    public class TestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
