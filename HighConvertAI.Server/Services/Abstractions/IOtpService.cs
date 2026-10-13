namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IOtpService
    {
        /// <summary>Generates a 6-digit OTP, saves its hash to DB, returns the plain OTP to send via email.</summary>
        Task<string> GenerateAndSaveAsync(Guid userId, string email, string purpose);

        /// <summary>Validates the submitted OTP. Returns true if valid, false otherwise. Increments attempt count.</summary>
        Task<(bool IsValid, string Error)> ValidateAsync(string email, string purpose, string submittedOtp);

        /// <summary>Invalidates all unused OTPs for this email + purpose (e.g. before issuing a new one).</summary>
        Task InvalidatePreviousAsync(string email, string purpose);
    }
}
