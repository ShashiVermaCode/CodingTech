using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static LEAComplaintsEmail.ILEAComplaintsEmailService;

namespace LEAComplaintsEmail
{
    public interface ILEAComplaintsEmailService
    {
        Task<bool> SendLEAComplaintsEmailAsync(
            List<string> toEmails,
            List<string> ccEmails,
            string subject,
            string htmlBody,
            List<string>? attachmentPaths = null);

    }
    public class LEAComplaintsEmailService : ILEAComplaintsEmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly string _displayName;
        private readonly ILogger<LEAComplaintsEmailService> _logger;
        private readonly IConfiguration _configuration;

        public LEAComplaintsEmailService(IConfiguration config, ILogger<LEAComplaintsEmailService> logger)
        {
            _smtpServer = config["emailforgetPassword:SmtpServer"]
                            ?? throw new InvalidOperationException("emailforgetPassword:SmtpServer is missing in appsettings.json");

            _smtpPort = config.GetValue<int?>("emailforgetPassword:SmtpPort")
                            ?? throw new InvalidOperationException("emailforgetPassword:SmtpPort is missing in appsettings.json");

            _username = config["emailforgetPassword:SenderEmail"]
                            ?? throw new InvalidOperationException("emailforgetPassword:SenderEmail is missing in appsettings.json");

            _password = config["emailforgetPassword:SenderPassword"]
                            ?? throw new InvalidOperationException("emailforgetPassword:SenderPassword is missing in appsettings.json");

            _displayName = config["emailforgetPassword:SenderName"]
                            ?? throw new InvalidOperationException("emailforgetPassword:SenderName is missing in appsettings.json");

            _logger = logger;
            _configuration = config;
        }
        public async Task<bool> SendLEAComplaintsEmailAsync(
            List<string> toEmails,
            List<string> ccEmails,
            string subject,
            string htmlBody,
            List<string>? attachmentPaths = null)
        {
            try
            {
                // ✅ Validate To Emails
                if (toEmails == null || !toEmails.Any(e => !string.IsNullOrWhiteSpace(e)))
                    throw new Exception("No valid recipient email found");

                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_username, _password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_username, _displayName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                // ✅ To
                foreach (var email in toEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.To.Add(new MailAddress(email.Trim()));

                // ✅ CC (safe check)
                if (ccEmails != null)
                {
                    foreach (var email in ccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                        message.CC.Add(new MailAddress(email.Trim()));
                }

                // ✅ Attachments
                if (attachmentPaths != null)
                {
                    foreach (var path in attachmentPaths.Where(p => File.Exists(p)))
                    {
                        message.Attachments.Add(new Attachment(path));
                        _logger.LogInformation($"Attached file: {Path.GetFileName(path)}");
                    }
                }

                await client.SendMailAsync(message);

                _logger.LogInformation($"Email sent to: {string.Join(", ", toEmails)}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Email sent successfully");
                Console.ResetColor();

                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP Error occurred");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"SMTP Error: {ex.Message}");
                Console.ResetColor();

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General Email Error");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();

                throw;
            }
        }
    }
}

