using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DigitalWallet.Domain.Entities;
using Microsoft.Extensions.Options;
using DigitalWallet.Application.Common.Models;

namespace DigitalWallet.Application.Helpers
{
    public class JwtTokenGenerator
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationHours;

        public JwtTokenGenerator(
            string secretKey,
            string issuer = "DigitalWallet",
            string audience = "DigitalWalletUsers",
            int expirationHours = 24)
        {
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
                throw new ArgumentException("Secret key must be at least 32 characters", nameof(secretKey));

            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
            _expirationHours = expirationHours;
        }

        public JwtTokenGenerator(IOptions<JwtSettings> options)
        {
        }

        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("phone", user.PhoneNumber),
                new Claim("kycLevel", user.KycLevel.ToString()),
                new Claim("status", user.Status.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_expirationHours),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a JWT token for an admin user
        /// </summary>
        public string GenerateAdminToken(AdminUser admin)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Name, admin.FullName),
                new Claim(ClaimTypes.Role, admin.Role.ToString()),
                new Claim("isAdmin", "true")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_expirationHours),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Validates a JWT token and returns the claims principal
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts user ID from token
        /// </summary>
        public Guid? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
    }
}