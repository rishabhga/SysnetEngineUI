using ManageEngineWebApp.Datacontext;
using Microsoft.AspNetCore.Mvc;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class DwonloadEXEController : Controller
    {
        public IActionResult Index()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exefile", "Sysnet_Trinetra.exe");

            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", "Sysnet_Trinetra.exe");
            }
            else
            {
                return NotFound("File not found.");
            }
           
        }
    }
}
