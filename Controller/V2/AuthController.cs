using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.V2.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MySQLService _db;

        // 🔐 Hardcoded JWT secret
        private readonly string _jwtSecret = "django-insecure-zo1%0k^q%b2t15i&41sgm5-4o4o^04l0vbhlht33u4cg-1dm3a";

        public AuthController(MySQLService db)
        {
            _db = db;
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { valid = false, message = "Missing or invalid Authorization header." });

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Unauthorized(new { valid = false, message = "Invalid token algorithm." });
                }

                // 🧩 Extract claims robustly
                var email = principal.FindFirst("email")?.Value
                         ?? principal.FindFirst(ClaimTypes.Email)?.Value
                         ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(email))
                    return Unauthorized(new { valid = false, message = "Token missing email claim." });

                var name = principal.FindFirst("name")?.Value
                        ?? principal.FindFirst(ClaimTypes.Name)?.Value
                        ?? email.Split('@')[0];

                var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "User";

                var users = await _db.GetDataAsync<UserDto>("Users");
                var user = users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    user = new UserDto
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        Role = role,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _db.PostDataAsync<UserDto>("Users", user);
                }

                var tokenString = GenerateToken(user.Email, user.Id.ToString(), user.Role ?? "User");

                return Ok(new
                {
                    valid = true,
                    message = "Token validated successfully.",
                    token = $"Bearer {tokenString}",
                    email = user.Email,
                    role = user.Role,
                    name
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { valid = false, message = $"Token validation failed: {ex.Message}" });
            }
        }

        private string GenerateToken(string email, string userId, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, email),
                    new Claim(JwtRegisteredClaimNames.Jti, userId),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddYears(1),
                Audience = "savetrack",
                Issuer = "craftmatrix.org",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
