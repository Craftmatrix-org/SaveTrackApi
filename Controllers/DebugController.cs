using Microsoft.AspNetCore.Mvc;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DebugController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string sysEnv = Environment.GetEnvironmentVariable("SYS_ENV");
            return Ok(new { SysEnv = sysEnv });

        }
    }
}
