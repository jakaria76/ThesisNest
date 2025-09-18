using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;
using ThesisNest.Models;

namespace ThesisNest.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

                using var client = new SmtpClient();
                // connect (use StartTls)
                var socketOption = _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, socketOption);

                // authenticate if provided
                if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername))
                {
                    await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"[SmtpEmailSender] Sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmtpEmailSender ERROR] {ex}");
                return false;
            }
        }
    }
}
