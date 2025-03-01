using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserTokenController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;

        public UserTokenController(MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("{email}")]
        public async Task<IActionResult> Get(string email)
        {
            var users = await _mysqlservice.GetDataAsync<UserDto>("Users");
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                user = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    Role = "User" // Assign a default role if null
                };
                await _mysqlservice.PostDataAsync<UserDto>("Users", user);
            }

            var tokenString = GenerateToken(user.Email, user.Id.ToString(), user.Role ?? "User");

            return Ok($"Bearer {tokenString}");
        }

        private string GenerateToken(string email, string userId, string role)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(role)) throw new ArgumentNullException(nameof(role));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set."));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, email),
                    new Claim(JwtRegisteredClaimNames.Jti, userId),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
