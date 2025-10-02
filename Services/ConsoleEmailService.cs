// using Microsoft.Extensions.Logging;

// namespace MediCare
// {
//     public class ConsoleEmailService : IEmailService
//     {
//         private readonly ILogger<ConsoleEmailService> _logger;

//         public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
//         {
//             _logger = logger;
//         }

//         public async Task SendConfirmationEmailAsync(string email, string confirmationLink)
//         {
//             _logger.LogInformation("=== EMAIL CONFIRMATION ===");
//             _logger.LogInformation("To: {Email}", email);
//             _logger.LogInformation("Confirmation Link: {Link}", confirmationLink);
//             _logger.LogInformation("===========================");
            
//             // In production, replace with actual email service:
//             // - SMTP (System.Net.Mail)
//             // - SendGrid
//             // - MailKit
//             // - AWS SES
//             await Task.CompletedTask;
//         }

//         public async Task SendPasswordResetEmailAsync(string email, string resetLink)
//         {
//             _logger.LogInformation("=== PASSWORD RESET ===");
//             _logger.LogInformation("To: {Email}", email);
//             _logger.LogInformation("Reset Link: {Link}", resetLink);
//             _logger.LogInformation("=======================");
            
//             await Task.CompletedTask;
//         }
//     }
// }

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace MediCare
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendConfirmationEmailAsync(string email, string confirmationLink)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var fromEmail = _config["Email:FromAddress"] ?? "noreply@medicare.com";
                var fromName = _config["Email:FromName"] ?? "Medicare System";
                var username = _config["Email:Username"];
                var password = _config["Email:Password"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "Confirm Your Medicare Account",
                    Body = $@"
                        <h2>Welcome to Medicare!</h2>
                        <p>Please confirm your email address by clicking the link below:</p>
                        <p><a href='{confirmationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
                        <p>Or copy this link to your browser:</p>
                        <p>{confirmationLink}</p>
                        <br>
                        <p>If you didn't create this account, please ignore this email.</p>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Confirmation email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}", email);
                throw new Exception("Failed to send email. Please try again later.");
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var fromEmail = _config["Email:FromAddress"] ?? "noreply@medicare.com";
                var fromName = _config["Email:FromName"] ?? "Medicare System";
                var username = _config["Email:Username"];
                var password = _config["Email:Password"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "Reset Your Medicare Password",
                    Body = $@"
                        <h2>Password Reset Request</h2>
                        <p>You requested to reset your password. Click the link below to set a new password:</p>
                        <p><a href='{resetLink}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                        <p>Or copy this link to your browser:</p>
                        <p>{resetLink}</p>
                        <br>
                        <p>If you didn't request this, please ignore this email.</p>
                        <p><strong>This link will expire in 2 hours.</strong></p>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                throw new Exception("Failed to send email. Please try again later.");
            }
        }
    }
}