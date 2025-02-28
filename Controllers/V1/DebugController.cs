using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Microsoft.AspNetCore.Authorization;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [AllowAnonymous]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;

        public DebugController(MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            return Ok(new { Token = token });
        }
    }
}
