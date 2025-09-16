using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaveTrackApi.Controller;

[ApiController]
[Route("[controller]")]
public class DebugController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok("Debug endpoint");
    }

    [HttpPost("generate-token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, request.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "regular")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new { Token = "Bearer " + tokenString });
    }

    [Authorize]
    [HttpGet("secure-endpoint")]
    public IActionResult SecureEndpoint()
    {
        return Ok("This is a secure endpoint. You are authenticated!");
    }
}

public class TokenRequest
{
    public string Email { get; set; }
}
