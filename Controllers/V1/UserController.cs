using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;

namespace Craftmatrix.org.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;
        // private readonly JwtService _jwtService;

        public UserController(MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
            // _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await _mysqlservice.GetDataAsync<UserDto>("Users");
            return Ok(user);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mysqlservice.DeleteDataAsync("Users", id);
            return Ok("Deleted Successfully");
        }
    }
}
