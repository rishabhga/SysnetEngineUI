using Microsoft.AspNetCore.Mvc;

namespace ManageEngineWebApp.Controllers
{
    public class DwonloadEXEController : Controller
    {
        public IActionResult Index()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "exefile", "SysnetManageEngineExe.exe");

            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", "SysnetManageEngineExe.exe");
            }
            else
            {
                return NotFound("File not found.");
            }
           
        }
    }
}
