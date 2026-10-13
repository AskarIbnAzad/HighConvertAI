using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IAuthService
    {
        Task<(bool Success, string Error)> RegisterAsync(RegisterRequestDto dto);
        Task<(bool Success, string Error, AuthResponseDto? Data)> VerifyOtpAsync(VerifyOtpRequestDto dto, string? deviceInfo);
        Task<(bool Success, string Error)> ResendOtpAsync(ResendOtpRequestDto dto);
        Task<(bool Success, string Error, AuthResponseDto? Data)> LoginAsync(LoginRequestDto dto, string? deviceInfo);
        Guid? GetCurrentUserId();
      
    }
}
