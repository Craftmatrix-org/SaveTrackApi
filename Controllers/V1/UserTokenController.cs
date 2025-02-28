using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UserTokenController> _logger;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _secretKey;

        public UserTokenController(MySQLService mySQLService, ILogger<UserTokenController> logger)
        {
            _mysqlservice = mySQLService;
            _logger = logger;

            _issuer = Environment.GetEnvironmentVariable("ISSUER") ?? "http://localhost:5184";
            _audience = Environment.GetEnvironmentVariable("AUDIENCE") ?? "http://localhost:7000";
            _secretKey = Environment.GetEnvironmentVariable("SECRETKEY");

            if (string.IsNullOrWhiteSpace(_secretKey) || _secretKey.Length < 32)
            {
                _logger.LogError("Invalid SECRETKEY! JWT generation will fail.");
                throw new InvalidOperationException("SECRETKEY is missing or too short.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("{email}")]
        public async Task<IActionResult> Get(string email = "aspirasrenz@gmail.com")
        {
            _logger.LogInformation("Generating token for email: {Email}", email);

            var users = await _mysqlservice.GetDataAsync<UserDto>("Users");
            var user = users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                user = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Email = email
                };
                await _mysqlservice.PostDataAsync<UserDto>("Users", user);
                _logger.LogInformation("New user created with email: {Email}", email);
            }

            var tokenString = GenerateToken(user.Email, user.Id.ToString());

            _logger.LogInformation("Token successfully generated for email: {Email}", email);

            return Ok(new { Token = tokenString });
        }

        private string GenerateToken(string email, string userId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("uid", userId)
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
