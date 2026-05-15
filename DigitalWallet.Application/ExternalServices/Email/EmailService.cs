using DigitalWallet.Application.DTOs;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DigitalWallet.Application.ExternalServices.Email
{
    public class EmailService : IEmailService
    {
        private readonly SendGridSettings _settings;

        public EmailService(IOptions<SendGridSettings> settings)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var client = new SendGridClient(_settings.ApiKey);
                var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
                var recipient = new EmailAddress(to);

                var msg = MailHelper.CreateSingleEmail(
                    from,
                    recipient,
                    subject,
                    null,
                    htmlBody
                );

                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"❌ SendGrid Error: {response.StatusCode}");
                    Console.WriteLine(responseBody);
                    return false;
                }

                Console.WriteLine($"✅ Email sent to {to}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email sending failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string to, string otpCode)
        {
            return await SendEmailAsync(to, "Your Digital Wallet OTP Code", EmailTemplates.GetOtpTemplate(otpCode));
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
        {
            return await SendEmailAsync(to, "Welcome to Digital Wallet 🎉", EmailTemplates.GetWelcomeTemplate(userName));
        }

        public async Task<bool> SendTransactionReceiptAsync(string to, string userName, TransactionReceiptDto receipt)
        {
            return await SendEmailAsync(to,
                $"Transaction Receipt - {receipt.TransactionId}",
                EmailTemplates.GetTransactionReceiptTemplate(userName, receipt));
        }

        public async Task<bool> SendLargeTransactionAlertAsync(string to, string userName, decimal amount, string currency)
        {
            return await SendEmailAsync(to,
                "⚠️ Large Transaction Alert",
                EmailTemplates.GetLargeTransactionAlertTemplate(userName, amount, currency));
        }
    }
}
