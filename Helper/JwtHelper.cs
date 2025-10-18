using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace Craftmatrix.org.Helpers
{
    public static class JwtHelper
    {
        public static string? GetJtiFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
