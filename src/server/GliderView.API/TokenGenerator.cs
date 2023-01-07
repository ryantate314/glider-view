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
        public string AuthSecurityKey { get; set; }
        public string RefreshSecurityKey { get; set; }

        /// <summary>
        /// Minutes
        /// </summary>
        public int AuthTokenLifetime { get; set; }
        public int RefreshTokenLifetime { get; set; }
    }

    public class Token
    {
        public string Value { get; set; }
        public DateTime ValidTo { get; set; }
    }

    public class TokenGenerator
    {
        private readonly JwtSettings _settings;
        private readonly SecurityKey _authKey;
        private readonly SecurityKey _refreshKey;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public TokenGenerator(JwtSettings settings)
        {
            _settings = settings;
            _authKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.AuthSecurityKey));
            _refreshKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.RefreshSecurityKey));

            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public Token GenerateAuthToken(User user, IEnumerable<string>? scopes = null)
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
                SigningCredentials = new SigningCredentials(_authKey, SecurityAlgorithms.HmacSha256Signature)
            };

            if (scopes != null)
            {
                tokenDescriptor.Subject.AddClaim(
                    new Claim("Scopes", String.Join(',', scopes))
                );
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return new Token()
            {
                Value = _tokenHandler.WriteToken(token),
                ValidTo = token.ValidTo
            };
        }

        public Token GenerateRefreshToken(User user, IEnumerable<string>? scopes = null)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                }),
                Expires = DateTime.UtcNow.AddMinutes(_settings.RefreshTokenLifetime),
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                SigningCredentials = new SigningCredentials(_refreshKey, SecurityAlgorithms.HmacSha256Signature)
            };

            if (scopes != null)
            {
                tokenDescriptor.Subject.AddClaim(
                    new Claim("Scopes", String.Join(',', scopes))
                );
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return new Token()
            {
                Value = _tokenHandler.WriteToken(token),
                ValidTo = token.ValidTo
            };
        }

    }
}
