using DigitalWallet.Application.DTOs;

namespace DigitalWallet.Application.ExternalServices.Email
{
    public static class EmailTemplates
    {
        public static string GetOtpTemplate(string otpCode)
        {
            return $@"<h2>Your OTP Code</h2>
                      <p><strong>{otpCode}</strong></p>
                      <p>Valid for 5 minutes.</p>";
        }

        public static string GetWelcomeTemplate(string userName)
        {
            return $@"<h2>Welcome {userName} 🎉</h2>
                      <p>Your Digital Wallet account is ready.</p>";
        }

        public static string GetTransactionReceiptTemplate(string userName, TransactionReceiptDto receipt)
        {
            return $@"<h2>Transaction Receipt</h2>
                      <p>User: {userName}</p>
                      <p>Amount: {receipt.Amount} {receipt.Currency}</p>
                      <p>Status: {receipt.Status}</p>";
        }

        public static string GetLargeTransactionAlertTemplate(string userName, decimal amount, string currency)
        {
            return $@"<h2>⚠️ Large Transaction Alert</h2>
                      <p>{userName}, a transaction of {amount} {currency} occurred.</p>";
        }
    }
}
