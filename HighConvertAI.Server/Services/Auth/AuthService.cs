using HighConvertAI.Server.Data;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;


namespace HighConvertAI.Server.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AuthService(
            ApplicationDbContext db,
            IOtpService otpService,
            IEmailService emailService,
            ITokenService tokenService,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _otpService = otpService;
            _emailService = emailService;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        // ── Register ──────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Error)> RegisterAsync(RegisterRequestDto dto)
        {
            var email = dto.Email.ToLowerInvariant();

            // Check if email already taken and verified
            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existing is not null)
            {
                if (existing.IsEmailVerified)
                    return (false, "An account with this email already exists.");

                // Unverified account exists — resend OTP instead of creating duplicate
                var plainOtp = await _otpService.GenerateAndSaveAsync(existing.Id, email, OtpPurpose.Register);
                await _emailService.SendOtpEmailAsync(email, existing.FullName, plainOtp);

                return (true, string.Empty); // Tell frontend to show OTP screen
            }

            // Validate password strength
            var passwordError = ValidatePasswordStrength(dto.Password);
            if (passwordError is not null)
                return (false, passwordError);

            // Create inactive user
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            

            // Assign default "Viewer" role (Id = 3)
            var viewerRole = await _db.Roles.FindAsync(3);
            if (viewerRole is not null)
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = viewerRole.Id });

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            if (dto.InitialPageScanID.HasValue)
            {
                var pageScan = await _db.PageScans.FirstOrDefaultAsync(p => p.Id == dto.InitialPageScanID);
                if (pageScan != null)
                {
                    pageScan.UserId = user.Id;
                    _db.PageScans.Update(pageScan);
                    await _db.SaveChangesAsync();
                }
            }

            // Generate OTP and send email
            var otp = await _otpService.GenerateAndSaveAsync(user.Id, email, OtpPurpose.Register);
            await _emailService.SendOtpEmailAsync(email, user.FullName, otp);

            return (true, string.Empty);
        }

        // ── Verify OTP ────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Error, AuthResponseDto? Data)> VerifyOtpAsync(
            VerifyOtpRequestDto dto, string? deviceInfo)
        {
            var email = dto.Email.ToLowerInvariant();

            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return (false, "User not found.", null);

            var (isValid, error) = await _otpService.ValidateAsync(email, OtpPurpose.Register, dto.Otp);

            if (!isValid)
                return (false, error, null);

            // Activate the account
            user.IsEmailVerified = true;
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Issue tokens
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var rawRefresh = await _tokenService.GenerateAndSaveRefreshTokenAsync(user.Id, deviceInfo);

            return (true, string.Empty, new AuthResponseDto
            {
                AccessToken = accessToken,
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = roles
                },
                RefreshToken = rawRefresh // Controller will put this in HttpOnly cookie
            });
        }

        // ── Resend OTP ────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Error)> ResendOtpAsync(ResendOtpRequestDto dto)
        {
            var email = dto.Email.ToLowerInvariant();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return (false, "User not found.");

            if (user.IsEmailVerified)
                return (false, "This account is already verified.");

            var otp = await _otpService.GenerateAndSaveAsync(user.Id, email, OtpPurpose.Register);
            await _emailService.SendOtpEmailAsync(email, user.FullName, otp);

            return (true, string.Empty);
        }

        // ── Password Validation ───────────────────────────────────────────────────

        private static string? ValidatePasswordStrength(string password)
        {
            if (password.Length < 8)
                return "Password must be at least 8 characters.";
            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter.";
            if (!password.Any(char.IsDigit))
                return "Password must contain at least one number.";
            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
                return "Password must contain at least one special character.";
            return null;
        }

        public async Task<(bool Success, string Error, AuthResponseDto? Data)> LoginAsync(LoginRequestDto dto, string? deviceInfo)
        {
            var email = dto.Email.ToLowerInvariant();

            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return (false, "Invalid email or password.", null);

            if (!user.IsEmailVerified)
                return (false, "Please verify your email first.", null);

            if (!user.IsActive)
                return (false, "Account is inactive.", null);

            var validPassword =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.PasswordHash);

            if (!validPassword)
                return (false, "Invalid email or password.", null);

            var roles =
                user.UserRoles
                    .Select(r => r.Role.Name)
                    .ToList();

            var accessToken =
                _tokenService.GenerateAccessToken(
                    user,
                    roles);

            var refreshToken =
                await _tokenService
                    .GenerateAndSaveRefreshTokenAsync(
                        user.Id,
                        deviceInfo);

            return (
                true,
                string.Empty,
                new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Roles = roles
                    }
                });
        }
        public Guid? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity!.IsAuthenticated)
                return null;

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (Guid.TryParse(userIdValue, out var userId))
                return userId;

            return null;
        }

       
    }
}
