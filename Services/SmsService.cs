using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;

namespace ThesisNest.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;

        public SmsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            var normalizedPhone = phoneNumber.StartsWith("0") ? "+88" + phoneNumber.Substring(1) : phoneNumber;

            var accountSid = _configuration["Authentication:Twilio:AccountSid"];
            var authToken = _configuration["Authentication:Twilio:AuthToken"];
            var fromNumber = _configuration["Authentication:Twilio:FromPhoneNumber"];

            TwilioClient.Init(accountSid, authToken);

            await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(normalizedPhone)
            );
        }
    }
}
