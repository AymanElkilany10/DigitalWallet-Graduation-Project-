namespace DigitalWallet.Application.Settings
{
    public class NotificationSettings
    {
        public bool SendOtpViaEmail { get; set; } = true;
        public bool SendOtpViaSms { get; set; } = true;
        public bool SendWelcomeEmail { get; set; } = true;
        public bool SendWelcomeSms { get; set; } = true;
        public bool SendTransactionAlerts { get; set; } = true;
        public decimal LargeTransactionThreshold { get; set; } = 5000m;
    }
}