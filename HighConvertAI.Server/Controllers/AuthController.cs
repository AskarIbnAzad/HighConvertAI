using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighConvertAI.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;   
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login( [FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deviceInfo =
                Request.Headers["User-Agent"].ToString();

            var (success, error, data) =
                await _authService.LoginAsync(
                    dto,
                    deviceInfo);

            if (!success)
                return Unauthorized(
                    new { message = error });

            Response.Cookies.Append(
                "refreshToken",
                data!.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                });

            return Ok(new AuthResponseDto
            {
                AccessToken = data.AccessToken,
                User = data.User
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, error) = await _authService.RegisterAsync(dto);

            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "Verification code sent to your email." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var (success, error, data) = await _authService.VerifyOtpAsync(dto, deviceInfo);

            if (!success)
                return BadRequest(new { message = error });

            Response.Cookies.Append("refreshToken", data!.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new AuthResponseDto
            {
                AccessToken = data.AccessToken,
                User = data.User
            });
        }

        [HttpPost("resend-otp")] 
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, error) = await _authService.ResendOtpAsync(dto);

            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "A new verification code has been sent." });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken =
                Request.Cookies["refreshToken"];

            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized();

            var result =
                await _tokenService.RefreshAsync(
                    refreshToken);

            if (result == null)
                return Unauthorized();

            return Ok(result);
        }

    }
}