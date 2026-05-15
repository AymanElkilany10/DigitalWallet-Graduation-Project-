using DigitalWallet.Application.ExternalServices.Email;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
    Task<bool> SendOtpEmailAsync(string to, string otpCode);
    Task<bool> SendWelcomeEmailAsync(string to, string userName);
    Task<bool> SendTransactionReceiptAsync(string to, string userName, TransactionReceiptDto receipt);
    Task<bool> SendLargeTransactionAlertAsync(string to, string userName, decimal amount, string currency);
}