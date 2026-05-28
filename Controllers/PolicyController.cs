using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ManageEngineWebApp.Filters;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ManageEngineWebApp.Datacontext;

namespace ManageEngineWebApp.Controllers
{
    [AuthFilter]
    public class PolicyController : BaseController
    {
        public PolicyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
            : base(httpClientFactory, configuration)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Templates()
        {
            try
            {
                using var client = GetClient();
                var response = await client.GetAsync($"{_baseUrl}/api/Policy/Templates");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                
                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                return Json(new { error = "An internal server error occurred." });
            }
        }
    }
}
