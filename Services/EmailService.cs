//using MailKit.Net.Smtp;
//using MimeKit;
//using System;
//using System.Threading.Tasks;

//namespace ThesisNest.Services
//{
//    public static class EmailService
//    {
//        /// <summary>
//        /// Sends an email asynchronously using Gmail SMTP.
//        /// </summary>
//        /// <param name="toEmail">Recipient email</param>
//        /// <param name="subject">Email subject</param>
//        /// <param name="body">HTML or plain text body</param>
//        /// <returns></returns>
//        public static async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
//        {
//            try
//            {
//                var message = new MimeMessage();

//                // Sender
//                message.From.Add(new MailboxAddress("ThesisNest", "yourgmail@gmail.com"));

//                // Recipient
//                message.To.Add(MailboxAddress.Parse(toEmail));

//                // Subject
//                message.Subject = subject;

//                // Body (HTML)
//                message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
//                {
//                    Text = body
//                };

//                using var client = new SmtpClient();

//                // Connect to Gmail SMTP
//                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

//                // Authenticate (Use Gmail App Password)
//                await client.AuthenticateAsync("yourgmail@gmail.com", "your-app-password");

//                // Send Email
//                await client.SendAsync(message);

//                // Disconnect
//                await client.DisconnectAsync(true);

//                Console.WriteLine($"[EmailService] Email sent to {toEmail}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[EmailService ERROR] Failed to send email to {toEmail}: {ex.Message}");
//                return false;
//            }
//        }
//    }
//}
