//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Gmail.v1;
//using Google.Apis.Gmail.v1.Data;
//using Google.Apis.Services;
//using Google.Apis.Util.Store;
//using MimeKit;
//using System;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace ThesisNest.Services
//{
//    public class GmailServiceHelper
//    {
//        private readonly GmailService _service;

//        // Private constructor used by the async factory
//        private GmailServiceHelper(GmailService service)
//        {
//            _service = service;
//        }

//        /// <summary>
//        /// Async factory to create GmailServiceHelper with OAuth authorization
//        /// </summary>
//        public static async Task<GmailServiceHelper> CreateAsync(string credentialsPath = "credentials.json", string tokenPath = "token.json")
//        {
//            if (!File.Exists(credentialsPath))
//                throw new FileNotFoundException($"Google credentials file not found: {credentialsPath}");

//            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);

//            var cred = await GoogleWebAuthorizationBroker.AuthorizeAsync(
//                GoogleClientSecrets.FromStream(stream).Secrets,
//                new[] { GmailService.Scope.GmailSend },
//                "user",
//                CancellationToken.None,
//                new FileDataStore(tokenPath, true)
//            );

//            var service = new GmailService(new BaseClientService.Initializer
//            {
//                HttpClientInitializer = cred,
//                ApplicationName = "ThesisNestApp",
//            });

//            return new GmailServiceHelper(service);
//        }

//        /// <summary>
//        /// Send email via Gmail
//        /// </summary>
//        public async Task SendEmailAsync(string toEmail, string subject, string body, string fromEmail = "jakariamahmud76@gmail.com", string fromName = "ThesisNest")
//        {
//            var msg = new MimeMessage();
//            msg.From.Add(new MailboxAddress(fromName, fromEmail));
//            msg.To.Add(MailboxAddress.Parse(toEmail));
//            msg.Subject = subject;
//            msg.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

//            using var ms = new MemoryStream();
//            msg.WriteTo(ms);

//            var encodedMsg = Convert.ToBase64String(ms.ToArray())
//                                  .Replace('+', '-')
//                                  .Replace('/', '_')
//                                  .Replace("=", "");

//            var gmailMsg = new Message { Raw = encodedMsg };
//            await _service.Users.Messages.Send(gmailMsg, "me").ExecuteAsync();
//        }
//    }
//}
