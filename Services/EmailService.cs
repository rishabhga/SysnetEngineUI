using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace ManageEngineWebApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
            int smtpPort = int.TryParse(smtpPortStr, out int port) ? port : 587;
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var smtpPass = _configuration["EmailSettings:SmtpPass"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                // In development, just log it
                System.Diagnostics.Debug.WriteLine($"EMAIL TO {email}: {subject}\n{message}");
                return;
            }

            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUser),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send email: {ex.Message}");
                // In development / fallback, we don't want to crash the app if email fails
            }
        }
    }
}
