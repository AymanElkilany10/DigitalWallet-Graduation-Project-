using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace DigitalWallet.Application.ExternalServices.SMS
{
    

    public class SmsService : ISmsService
    {
        private readonly TwilioSettings _settings;

        public SmsService(IOptions<TwilioSettings> settings)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));

            // Initialize Twilio
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // Ensure phone number is in E.164 format
                if (!phoneNumber.StartsWith("+"))
                {
                    // Assuming Egyptian phone numbers
                    phoneNumber = "+20" + phoneNumber.TrimStart('0');
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_settings.FromPhoneNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                Console.WriteLine($"✅ SMS sent to {phoneNumber}. SID: {messageResource.Sid}");
                return messageResource.Status != MessageResource.StatusEnum.Failed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SMS sending failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOtpAsync(string phoneNumber, string otpCode)
        {
            var message = $"Digital Wallet: Your OTP code is {otpCode}. Valid for 5 minutes. Do not share this code.";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendWelcomeSmsAsync(string phoneNumber, string userName)
        {
            var message = $"Welcome to Digital Wallet, {userName}! Your account is now active. Start sending money instantly.";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendTransactionAlertAsync(string phoneNumber, decimal amount, string currency, string type)
        {
            var message = $"Digital Wallet: {type} of {amount:N2} {currency} completed at {DateTime.UtcNow:HH:mm} UTC.";
            return await SendSmsAsync(phoneNumber, message);
        }
    }
}