using ManageEngineWebApp.Attributes;
using ManageEngineWebApp.Datacontext;
using ManageEngineWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class InstallerController : Controller
    {

        private readonly IWebHostEnvironment _env;
        public InstallerController(IWebHostEnvironment env) 
        {
                _env = env;
        }


        [HttpPost]
        [DynamicPermission("Installer.Download", "Download Installer")]
        public IActionResult DownloadInstaller([FromBody] InstallerRequest requestData)
        {
            // Sanitize the location name
            string safeLocation = requestData.LocationName.Replace(" ", "_").Replace("/", "").Replace("\\", "").Replace("..", "");

            // Construct file name as per your format
            string fileName = $"{safeLocation}_C{requestData.CompanyId}_G{requestData.GroupId}_Installer.exe";

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ClientSoftware", fileName);

            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            else
            {
                return NotFound("Installer file not found.");
            }
        }


        [HttpPost]
        [DynamicPermission("Installer.Generate", "Generate Installer")]
        public async Task<IActionResult> GenerateInstaller([FromBody] InstallerRequest requestData)
        {

            try
            {

                var testLogPath1 = Path.Combine(_env.WebRootPath, "installersoftware", "test_log1.txt");
                System.IO.File.AppendAllText(testLogPath1, $"[{DateTime.Now}] GenerateInstaller called\n");
                // Step 1: Update config.txt
                UpdateConfigFile(requestData.CompanyId, requestData.GroupId, requestData.LocationId);

                // Step 2: Compile Installer
                CompileInnoSetup();
                // Step 3: Return the actual file
                //var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "installersoftware", "Output", "setup.exe");
                var filePath = Path.Combine(_env.WebRootPath, "installersoftware", "Output", "Manageengine.exe");
                string folderPath = Path.Combine("C:\\SysnetEngineWeb\\wwwroot\\Installersoftware");
                // var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "installersoftware", "Output", "setup.exe");


                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("File not found.");
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileName = $"{requestData.LocationName + "_" + requestData.CompanyId}.exe";
                return File(fileBytes, "application/octet-stream", fileName);


            }
            catch (Exception ex)
            {
                var debugErrorPath = Path.Combine(Directory.GetCurrentDirectory(), "installersoftware", "debug_error.txt");
                System.IO.File.AppendAllText(debugErrorPath, $"[{DateTime.Now}] ERROR before code: {ex.Message}\n");
                string errorLogPath = Path.Combine(_env.WebRootPath, "installersoftware", "errorlog", "error_log.txt");
                System.IO.File.AppendAllText(errorLogPath,
                    $"[{DateTime.Now}] ERROR in GenerateInstaller: {ex.Message}\nStackTrace: {ex.StackTrace}\n");
                return StatusCode(100, "Internal server error");

               
            }

        }
        private void UpdateConfigFile(int companyId, int groupId, int locationId)
        {

            string rootPath = Path.Combine(_env.WebRootPath, "installersoftware");
            //string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "installersoftware");
            //string rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "installersoftware");


            // Ensure folder exists
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            string configPath = Path.Combine(rootPath, "config.txt");

            var lines = new List<string>
            {
                $"CompanyId={companyId}",
                $"GroupId={groupId}",
                $"LocationId={locationId}"
            };

            System.IO.File.WriteAllLines(configPath, lines);
            System.IO.File.AppendAllText(Path.Combine(rootPath, "debug_log.txt"), $"Config updated at {DateTime.Now}\nPath: {configPath}\n");

        }

        //runing method
        //private void CompileInnoSetup()
        //{


        //    string innoPath = @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe";
        //    string scriptPath = Path.Combine(_env.WebRootPath, "installersoftware", "installer.iss");
           
        //    string logPath = Path.Combine(_env.WebRootPath, "installersoftware", "compile_log.txt");
        //    //string innoPath = @"C:\Program Files (x86)\Inno Setup 6\ISCC.exe"; // Adjust if installed elsewhere
        //    //string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "installersoftware", "installer.iss");

        //    //string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "installersoftware", "installer.iss");

        //    ProcessStartInfo startInfo = new()
        //    {
        //        FileName = innoPath,
        //        Arguments = $"\"{scriptPath}\"",
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true
        //    };

        //    using (var process = Process.Start(startInfo))
        //    {
        //        string output = process.StandardOutput.ReadToEnd();
        //        string error = process.StandardError.ReadToEnd();
                

        //        process.WaitForExit();
        //    }
        //}


        //testing method
        private void CompileInnoSetup()
        {
            // ✅ Change 1: Inno Setup ko project ke andar deploy karo
            string innoPath = Path.Combine(_env.WebRootPath, "installersoftware", "tools", "ISCC.exe"); // NEW
            string scriptPath = Path.Combine(_env.WebRootPath, "installersoftware", "installer.iss");
            string logPath = Path.Combine(_env.WebRootPath, "installersoftware", "compile_log.txt");

            if (!System.IO.File.Exists(innoPath))
            {
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR: ISCC.exe not found at {innoPath}\n");
                throw new FileNotFoundException("ISCC.exe not found.", innoPath);
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = innoPath,
                Arguments = $"\"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // ✅ Change 2: Save log
                string logText = $"[{DateTime.Now}] ExitCode: {process.ExitCode}\nOutput:\n{output}\nError:\n{error}\n";
                System.IO.File.AppendAllText(logPath, logText);

                if (process.ExitCode != 0)
                {
                    string innologPath = Path.Combine(_env.WebRootPath, "installersoftware", "inno_log.txt");
                    System.IO.File.AppendAllText(innologPath, $"[{DateTime.Now}] ERROR: ISCC.exe not found at {innoPath}\n");
                    throw new Exception($"Inno Setup compilation failed. ExitCode={process.ExitCode}");
                }
            }
        }
        }
}
