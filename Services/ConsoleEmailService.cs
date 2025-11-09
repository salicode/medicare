
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

        public async Task SendAppointmentConfirmationAsync(string email, string appointmentDetails)
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
                    Subject = "Medicare - Appointment Confirmation",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background: linear-gradient(135deg, #007bff, #0056b3); color: white; padding: 20px; text-align: center;'>
                                <h1 style='margin: 0;'>Medicare Healthcare</h1>
                                <p style='margin: 5px 0 0 0; opacity: 0.9;'>Your Health, Our Priority</p>
                            </div>
                            
                            <div style='padding: 30px; background-color: #f8f9fa;'>
                                <h2 style='color: #007bff; margin-top: 0;'>Appointment Notification</h2>
                                
                                <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #007bff;'>
                                    {appointmentDetails}
                                </div>
                                
                                <div style='margin-top: 25px; padding: 15px; background-color: #e7f3ff; border-radius: 5px;'>
                                    <h3 style='color: #0056b3; margin-top: 0;'>Important Reminders:</h3>
                                    <ul style='color: #555;'>
                                        <li>Please arrive 10-15 minutes before your scheduled time</li>
                                        <li>Bring your ID and insurance information</li>
                                        <li>Have your symptoms and medical history ready</li>
                                        <li>Contact us immediately if you need to reschedule</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div style='background-color: #343a40; color: white; padding: 20px; text-align: center;'>
                                <p style='margin: 0; opacity: 0.8;'>
                                    &copy; {DateTime.Now.Year} Medicare Healthcare System. All rights reserved.<br>
                                    Contact: support@medicare.com | Phone: (555) 123-4567
                                </p>
                            </div>
                        </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Appointment confirmation email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment confirmation email to {Email}", email);
                throw new Exception("Failed to send appointment confirmation email. Please try again later.");
            }
        }
    }
}