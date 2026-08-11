using HospitalManagementSystem.API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HospitalManagementSystem.API.Services
{
    public class EmailOptions
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Hospital Management System";
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toAddress, string toName, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpUser) || string.IsNullOrWhiteSpace(_options.SmtpPassword))
            {
                _logger.LogWarning("Email not configured - skipping send to {ToAddress}: {Subject}", toAddress, subject);
                return;
            }

            try
            {
                var message = new MimeMessage();
                var fromAddress = string.IsNullOrWhiteSpace(_options.FromAddress) ? _options.SmtpUser : _options.FromAddress;
                message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
                message.To.Add(new MailboxAddress(toName, toAddress));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var client = new SmtpClient();
                await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Email is a notification, not core functionality - a failed send should
                // never break the appointment flow that triggered it.
                _logger.LogWarning(ex, "Failed to send email to {ToAddress}: {Subject}", toAddress, subject);
            }
        }
    }
}
