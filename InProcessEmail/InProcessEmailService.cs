using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using InProcessEmail.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace InProcessEmail
{
    public interface IInProcessEmailService
    {
        Task<bool> SendInprocessEmailAsync(List<string> toEmail, List<string> ccEmails, string subject, string htmlBody, List<string>? attachmentPaths = null);
    }

    public class InProcessEmailService : IInProcessEmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _username;
        private readonly string _password;
        private readonly string _displayName;
        private readonly ILogger<InProcessEmailService> _logger;
        private readonly IConfiguration _configuration;

        public InProcessEmailService(IConfiguration config, ILogger<InProcessEmailService> logger)
        {
            _smtpServer = config["emailforgetPassword:SmtpServer"]!;
            _smtpPort = int.Parse(config["emailforgetPassword:SmtpPort"]!);
            _username = config["emailforgetPassword:SenderEmail"]!;
            _password = config["emailforgetPassword:SenderPassword"]!;
            _displayName = config["emailforgetPassword:SenderName"]!;
            _logger = logger;
            _configuration = config;

        }

        public async Task<bool> SendInprocessEmailAsync(List<string> toEmail, List<string> ccEmails, string subject, string htmlBody, List<string>? attachmentPaths = null)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_username, _password)
                };

                using var message = new MailMessage();
                message.From = new MailAddress(_username, _displayName);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                // ✅ To
                foreach (var email in toEmail.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.To.Add(new MailAddress(email.Trim()));

                // ✅ CC
                foreach (var email in ccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    message.CC.Add(new MailAddress(email.Trim()));

                // ✅ Attachments - Payout + VPA Excel
                if (attachmentPaths != null)
                {
                    foreach (var path in attachmentPaths.Where(p => File.Exists(p)))
                    {
                        message.Attachments.Add(new Attachment(path));
                        Console.WriteLine($"📎 Attached: {Path.GetFileName(path)}");
                    }
                }

                await client.SendMailAsync(message);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Email sent to: {string.Join(", ", toEmail)}");
                Console.ResetColor();
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError($"[SMTP Error] {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"SMTP Error: {ex.Message}");
                Console.ResetColor();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Email Error] {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"General Error: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }
    }

}
