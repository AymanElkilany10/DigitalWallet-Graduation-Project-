namespace DigitalWallet.Application.ExternalServices.SMS
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendOtpAsync(string phoneNumber, string otpCode);
        Task<bool> SendWelcomeSmsAsync(string phoneNumber, string userName);
        Task<bool> SendTransactionAlertAsync(string phoneNumber, decimal amount, string currency, string type);
    }
}