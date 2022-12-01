using GliderView.Service.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GliderView.API
{
    public class JwtSettings
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecurityKey { get; set; }

        /// <summary>
        /// Minutes
        /// </summary>
        public int AuthTokenLifetime { get; set; }
    }

    public class Token
    {
        public string Value { get; set; }
        public DateTime ValidTo { get; set; }
    }

    public class TokenGenerator
    {
        private readonly JwtSettings _settings;
        private readonly SecurityKey _key;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public TokenGenerator(JwtSettings settings)
        {
            _settings = settings;
            _key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.SecurityKey));

            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public Token GenerateAuthToken(User user)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                }),
                Expires = DateTime.UtcNow.AddMinutes(_settings.AuthTokenLifetime),
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature)
            };

            if (user.Role == User.ROLE_ADMIN)
            {
                tokenDescriptor.Subject.AddClaims(
                    Scopes.Roles.Admin.Select(x => new Claim(x, ""))
                );
            }

            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return new Token()
            {
                Value = _tokenHandler.WriteToken(token),
                ValidTo = token.ValidTo
            };
        }
    }
}
