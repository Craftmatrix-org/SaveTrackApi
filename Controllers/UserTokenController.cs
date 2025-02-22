using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserTokenController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;
        private readonly JwtService _jwtService;

        public UserTokenController(JwtService jwtService, MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
            _jwtService = jwtService;
        }

        [HttpPost]
        [Route("{email}")]
        public async Task<IActionResult> Get(string email = "aspirasrenz@gmail.com")
        {
            var users = await _mysqlservice.GetDataAsync<UserDto>("Users");
            var user = users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                UserDto userDto = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Email = email
                };
                await _mysqlservice.PostDataAsync<UserDto>("Users", userDto);
            }

            JWTDto jWTDto = new JWTDto
            {
                email = email,
                Uid = user.Id,
            };

            var token = _jwtService.GenerateToken(jWTDto);
            return Ok(token);
        }
    }
}
