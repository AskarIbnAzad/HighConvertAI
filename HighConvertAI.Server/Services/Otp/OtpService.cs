using HighConvertAI.Server.Data;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Services.Abstractions;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;


namespace HighConvertAI.Server.Services.Otp
{
    public class OtpService : IOtpService
    {
        private const int OtpExpiryMinutes = 10;
        private const int MaxAttempts = 3;

        private readonly ApplicationDbContext _db;

        public OtpService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── Generate ─────────────────────────────────────────────────────────────

        public async Task<string> GenerateAndSaveAsync(Guid userId, string email, string purpose)
        {
            // Invalidate any previous unused OTPs for this email + purpose
            await InvalidatePreviousAsync(email, purpose);

            // Cryptographically random 6-digit OTP
            var plain = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var otp = new OtpCode
            {
                UserId = userId,
                Email = email.ToLowerInvariant(),
                CodeHash = BCrypt.Net.BCrypt.HashPassword(plain),
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                IsUsed = false,
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.OtpCodes.Add(otp);
            await _db.SaveChangesAsync();

            return plain; // Only plain value returned here — never stored
        }

        // ── Validate ─────────────────────────────────────────────────────────────

        public async Task<(bool IsValid, string Error)> ValidateAsync(
            string email, string purpose, string submittedOtp)
        {
            var record = await _db.OtpCodes
                .Where(o => o.Email == email.ToLowerInvariant()
                         && o.Purpose == purpose
                         && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (record is null)
                return (false, "No active OTP found. Please request a new one.");

            if (DateTime.UtcNow > record.ExpiresAt)
            {
                record.IsUsed = true;
                await _db.SaveChangesAsync();
                return (false, "OTP has expired. Please request a new one.");
            }

            if (record.AttemptCount >= MaxAttempts)
            {
                record.IsUsed = true;
                await _db.SaveChangesAsync();
                return (false, "Too many failed attempts. Please request a new OTP.");
            }

            // Increment attempt before checking — prevents timing attacks
            record.AttemptCount++;

            if (!BCrypt.Net.BCrypt.Verify(submittedOtp, record.CodeHash))
            {
                await _db.SaveChangesAsync();
                int remaining = MaxAttempts - record.AttemptCount;
                return (false, $"Invalid OTP. {remaining} attempt(s) remaining.");
            }

            // Valid — mark as used
            record.IsUsed = true;
            await _db.SaveChangesAsync();

            return (true, string.Empty);
        }

        // ── Invalidate Previous ───────────────────────────────────────────────────

        public async Task InvalidatePreviousAsync(string email, string purpose)
        {
            var previous = await _db.OtpCodes
                .Where(o => o.Email == email.ToLowerInvariant()
                         && o.Purpose == purpose
                         && !o.IsUsed)
                .ToListAsync();

            foreach (var o in previous)
                o.IsUsed = true;

            if (previous.Any())
                await _db.SaveChangesAsync();
        }
    }
}
