namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string fullName, string otp);
    }
}
