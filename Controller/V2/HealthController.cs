using Microsoft.AspNetCore.Mvc;

namespace Craftmatrix.org.Controllers
{
    public class HealthController : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "Healthy" });
        }
    }
}
