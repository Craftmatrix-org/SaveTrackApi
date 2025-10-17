using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Craftmatrix.org.Services;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.V2.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MySQLService _db;
        private readonly string _jwtSecret = "django-insecure-zo1%0k^q%b2t15i&41sgm5-4o4o^04l0vbhlht33u4cg-1dm3a";

        public AuthController(MySQLService db)
        {
            _db = db;
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken()
        {
            // 1. Get the Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { valid = false, message = "Missing or invalid Authorization header." });

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                // 2. Setup JWT validation
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // no time delay
                };

                // 3. Validate token
                var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);

                // Optionally: check if token is actually JWT
                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Unauthorized(new { valid = false, message = "Invalid token algorithm." });
                }

                // 4. Success
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal.FindFirst(ClaimTypes.Name)?.Value;

                var users = await _db.GetDataAsync<UserDto>("Users");
                var user = users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    user = new UserDto
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        Role = "User" // Assign a default role if null
                    };
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _db.PostDataAsync<UserDto>("Users", user);
                }
                var tokenString = GenerateToken(user.Email, user.Id.ToString(), user.Role ?? "User");
                return Ok($"Bearer {tokenString}");

            }
            catch (Exception ex)
            {
                return Unauthorized(new { valid = false, message = $"Token validation failed: {ex.Message}" });
            }
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
