using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;
        private readonly JwtService _jwtService;

        public DebugController(JwtService jwtService, MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string sysEnv = Environment.GetEnvironmentVariable("SYS_ENV");
            return Ok(new { SysEnv = sysEnv });
        }
    }
}
