using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, IList<string> roles);
        Task<string> GenerateAndSaveRefreshTokenAsync(Guid userId, string? deviceInfo = null);
        Task RevokeAllRefreshTokensAsync(Guid userId);
        Task<AuthResponseDto?> RefreshAsync(string rawRefreshToken);

    }
}
