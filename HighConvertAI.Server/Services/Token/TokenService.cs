using HighConvertAI.Server.Data;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using HighConvertAI.Server.Models.DTO;


namespace HighConvertAI.Server.Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _db;

        public TokenService(IConfiguration config, ApplicationDbContext db)
        {
            _config = config;
            _db = db;
        }

        // ── Access Token (JWT) ────────────────────────────────────────────────────

        public string GenerateAccessToken(User user, IList<string> roles)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(double.Parse(jwt["AccessTokenExpiryMinutes"]!));
            //var expiry = DateTime.UtcNow.AddSeconds(15);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name,  user.FullName),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── Refresh Token ─────────────────────────────────────────────────────────

        public async Task<string> GenerateAndSaveRefreshTokenAsync(Guid userId, string? deviceInfo = null)
        {
            var jwt = _config.GetSection("Jwt");
            var expiry = DateTime.UtcNow.AddDays(double.Parse(jwt["RefreshTokenExpiryDays"]!));

            // Generate cryptographically random raw token
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(rawToken),
                ExpiresAt = expiry,
                IsRevoked = false,
                DeviceInfo = deviceInfo,
                CreatedAt = DateTime.UtcNow
            };

            _db.RefreshTokens.Add(entity);
            await _db.SaveChangesAsync();

            return rawToken; // Return raw — this is what goes in the HttpOnly cookie
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            var tokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var t in tokens)
                t.IsRevoked = true;

            await _db.SaveChangesAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        public static string HashToken(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }

        public async Task<AuthResponseDto?> RefreshAsync(string rawRefreshToken)
        {
            var tokenHash =
                HashToken(rawRefreshToken);

            var refreshToken =
                await _db.RefreshTokens
                    .Include(x => x.User)
                        .ThenInclude(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(x =>
                        x.TokenHash == tokenHash &&
                        !x.IsRevoked);

            if (refreshToken == null)
                return null;

            if (refreshToken.IsExpired)
                return null;

            var user =
                refreshToken.User;

            var roles =
                user.UserRoles
                    .Select(x => x.Role.Name)
                    .ToList();

            var accessToken =
                GenerateAccessToken(user, roles);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = roles
                }
            };
        }
    }

}
