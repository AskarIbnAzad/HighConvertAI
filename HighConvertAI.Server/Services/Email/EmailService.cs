using HighConvertAI.Server.Services.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HighConvertAI.Server.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string fullName, string otp)
        {
            var subject = "Your Verification Code — HighConvertAI";
            var body = BuildOtpEmailBody(fullName, otp);
            await SendAsync(toEmail, fullName, subject, body);
        }

        // ── Core Send ─────────────────────────────────────────────────────────────

        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var settings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                settings["SenderName"],
                settings["SenderEmail"]));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                settings["SmtpHost"],
                int.Parse(settings["SmtpPort"]!),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                settings["SmtpUser"],
                settings["SmtpPass"]);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        // ── Email Template ────────────────────────────────────────────────────────

        private static string BuildOtpEmailBody(string fullName, string otp) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; background:#f4f4f4; padding: 40px 0;">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td align="center">
                <table width="500" style="background:#fff; border-radius:8px; padding:40px;">
                  <tr>
                    <td>
                      <h2 style="color:#1a1a1a; margin-bottom:8px;">Verify your email</h2>
                      <p style="color:#555;">Hi {fullName}, use the code below to complete your registration.</p>
                      <div style="text-align:center; margin:32px 0;">
                        <span style="
                          font-size:36px;
                          font-weight:bold;
                          letter-spacing:12px;
                          color:#4f46e5;
                          background:#f0f0ff;
                          padding:16px 28px;
                          border-radius:8px;
                          display:inline-block;">
                          {otp}
                        </span>
                      </div>
                      <p style="color:#888; font-size:13px;">
                        This code expires in <strong>10 minutes</strong>.<br/>
                        If you didn't request this, you can safely ignore this email.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }

}
