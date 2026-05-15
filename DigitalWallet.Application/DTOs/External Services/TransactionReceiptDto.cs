namespace DigitalWallet.Application.ExternalServices.Email
{
    public class TransactionReceiptDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
