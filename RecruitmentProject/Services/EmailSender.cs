using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using RecruitmentProject.Data;

namespace RecruitmentProject.Services
{
    public class EmailSender : IEmailSender<ApplicationUser>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            SendEmailAsync(email, "Reset Password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            SendEmailAsync(email, "Reset Password", $"Your password reset code is: {resetCode}");

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var host = _configuration["EmailSender:Host"];
            var userName = _configuration["EmailSender:UserName"];
            var password = _configuration["EmailSender:Password"];

            // If SMTP isn't configured yet (e.g. local development), just log the message
            // so the reset/confirmation link can still be copied and tested manually.
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(userName))
            {
                _logger.LogWarning(
                    "EmailSender is not fully configured. Logging email instead of sending it.\nTo: {Email}\nSubject: {Subject}\nBody:\n{HtmlMessage}",
                    email, subject, htmlMessage);
                return;
            }

            var port = _configuration.GetValue<int>("EmailSender:Port", 587);
            var enableSsl = _configuration.GetValue<bool>("EmailSender:EnableSsl", true);
            var fromAddress = _configuration["EmailSender:FromAddress"] ?? userName;
            var fromName = _configuration["EmailSender:FromName"] ?? "Recruitment Team";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(userName, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}
