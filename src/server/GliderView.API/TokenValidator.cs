using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;

namespace GliderView.API
{
    public class TokenValidator
    {
        private readonly TokenValidationParameters _tokenParams;
        private readonly JwtSecurityTokenHandler _handler;
        private readonly ILogger<TokenValidator> _logger;

        public TokenValidator(JwtSettings settings, ILogger<TokenValidator> logger)
        {
            _tokenParams = new TokenValidationParameters()
            {
                ValidAudience = settings.Audience,
                ValidIssuer = settings.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.RefreshSecurityKey))
            };

            _handler = new JwtSecurityTokenHandler();

            _logger = logger;
        }

        public bool TryValidateToken(string token, out ClaimsPrincipal? user)
        {
            try
            {
                user = _handler.ValidateToken(token, _tokenParams, out _);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error validating token: " + ex.Message);

                user = null;
                return false;
            }

            return true;
        }

        public Guid GetUserId(ClaimsPrincipal user)
        {
            return Guid.Parse(
                user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
            );
        }
    }
}
