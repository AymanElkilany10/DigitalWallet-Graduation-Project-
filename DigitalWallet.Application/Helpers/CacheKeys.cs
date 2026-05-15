namespace DigitalWallet.Application.Helpers
{
    public static class CacheKeys
    {
        // User caching
        public static string UserProfile(Guid userId) => $"user:profile:{userId}";
        public static string UserByEmail(string email) => $"user:email:{email}";
        public static string UserByPhone(string phone) => $"user:phone:{phone}";

        // Wallet caching
        public static string UserWallets(Guid userId) => $"wallet:user:{userId}";
        public static string WalletBalance(Guid walletId) => $"wallet:balance:{walletId}";
        public static string Wallet(Guid walletId) => $"wallet:{walletId}";

        // Transaction caching
        public static string WalletTransactions(Guid walletId, int pageNumber, int pageSize)
            => $"transactions:wallet:{walletId}:page:{pageNumber}:size:{pageSize}";
        public static string Transaction(Guid transactionId) => $"transaction:{transactionId}";

        // Biller caching
        public static string AllBillers() => "billers:all";
        public static string ActiveBillers() => "billers:active";
        public static string BillersByCategory(string category) => $"billers:category:{category}";

        // 🆕 Bill payment caching
        public static string UserBillPayments(Guid userId, int pageNumber, int pageSize)
            => $"bill-payments:user:{userId}:page:{pageNumber}:size:{pageSize}";
        public static string BillPayment(Guid paymentId) => $"bill-payment:{paymentId}";

        // Notification caching
        public static string UnreadNotificationCount(Guid userId) => $"notifications:unread:{userId}";
        public static string UserNotifications(Guid userId, int pageNumber)
            => $"notifications:user:{userId}:page:{pageNumber}";

        // Exchange rates caching
        public static string ExchangeRate(string from, string to) => $"exchange:rate:{from}:{to}";
        public static string AllExchangeRates() => "exchange:rates:all";

        // Patterns for invalidation
        public static string UserPattern(Guid userId) => $"user:*:{userId}*";
        public static string WalletPattern(Guid walletId) => $"wallet:*:{walletId}*";
        public static string TransactionPattern(Guid walletId) => $"transactions:wallet:{walletId}*";
        public static string BillPaymentPattern(Guid userId) => $"bill-payments:user:{userId}*";
    }
}