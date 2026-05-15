using Bogus;
using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.DTOs.BillPayment;
using DigitalWallet.Application.DTOs.Transfer;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Tests.Fixtures
{
    public static class TestDataBuilder
    {
        private static readonly Faker _faker = new("en");

        // ── Entities ──────────────────────────────────────────

        public static User CreateUser(Action<User>? configure = null)
        {
            var user = new Faker<User>()
                .RuleFor(u => u.Id, f => f.Random.Guid())
                .RuleFor(u => u.FullName, f => f.Name.FullName())
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.PhoneNumber, f => $"010{f.Random.Number(10000000, 99999999)}")
                .RuleFor(u => u.PasswordHash, f => f.Random.AlphaNumeric(64))
                .RuleFor(u => u.Salt, f => f.Random.AlphaNumeric(32))
                .RuleFor(u => u.KycLevel, _ => KycLevel.Basic)
                .RuleFor(u => u.Status, _ => UserStatus.Active)
                .RuleFor(u => u.CreatedAt, f => f.Date.Past(1))
                .RuleFor(u => u.LastLoginAt, f => f.Date.Recent(7))
                .Generate();

            configure?.Invoke(user);
            return user;
        }

        public static Wallet CreateWallet(
            Guid? userId = null, string currency = "EGP",
            decimal balance = 10_000m, Action<Wallet>? configure = null)
        {
            var wallet = new Faker<Wallet>()
                .RuleFor(w => w.Id, f => f.Random.Guid())
                .RuleFor(w => w.UserId, _ => userId ?? Guid.NewGuid())
                .RuleFor(w => w.CurrencyCode, _ => currency)
                .RuleFor(w => w.Balance, _ => balance)
                .RuleFor(w => w.DailyLimit, _ => 5_000m)
                .RuleFor(w => w.MonthlyLimit, _ => 20_000m)
                .RuleFor(w => w.CreatedAt, f => f.Date.Past(1))
                .Generate();

            configure?.Invoke(wallet);
            return wallet;
        }

        public static OtpCode CreateOtpCode(
            Guid? userId = null, string code = "123456",
            OtpType type = OtpType.Login,
            bool isUsed = false, bool isExpired = false)
        {
            return new OtpCode
            {
                Id = Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                Code = code,
                Type = type,
                ExpiresAt = isExpired ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(5),
                IsUsed = isUsed,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ✅ FIX: Use TransactionStatus (no TransferStatus enum in project)
        public static Transfer CreateTransfer(
            Guid? senderWalletId = null, Guid? receiverWalletId = null, decimal amount = 500m)
        {
            return new Faker<Transfer>()
                .RuleFor(t => t.Id, f => f.Random.Guid())
                .RuleFor(t => t.SenderWalletId, _ => senderWalletId ?? Guid.NewGuid())
                .RuleFor(t => t.ReceiverWalletId, _ => receiverWalletId ?? Guid.NewGuid())
                .RuleFor(t => t.Amount, _ => amount)
                .RuleFor(t => t.CurrencyCode, _ => "EGP")
                .RuleFor(t => t.Status, _ => TransactionStatus.Success)
                .Generate();
        }

        public static Transaction CreateTransaction(
            Guid? walletId = null,
            TransactionType type = TransactionType.Transfer,
            decimal amount = 500m)
        {
            return new Faker<Transaction>()
                .RuleFor(t => t.Id, f => f.Random.Guid())
                .RuleFor(t => t.WalletId, _ => walletId ?? Guid.NewGuid())
                .RuleFor(t => t.Type, _ => type)
                .RuleFor(t => t.Amount, _ => amount)
                .RuleFor(t => t.CurrencyCode, _ => "EGP")
                .RuleFor(t => t.Status, _ => TransactionStatus.Success)
                .RuleFor(t => t.Description, f => f.Lorem.Sentence(5))
                .RuleFor(t => t.Reference, f => f.Random.AlphaNumeric(12).ToUpper())
                .RuleFor(t => t.CreatedAt, f => f.Date.Recent(7))
                .Generate();
        }

        // ✅ FIX: Removed BillerCategory — Category field omitted (uses default)
        public static Biller CreateBiller(bool isActive = true)
        {
            return new Faker<Biller>()
                .RuleFor(b => b.Id, f => f.Random.Guid())
                .RuleFor(b => b.Name, f => f.Company.CompanyName())
                .RuleFor(b => b.IsActive, _ => isActive)
                .RuleFor(b => b.CreatedAt, f => f.Date.Past(1))
                .Generate();
        }

        public static FakeBankAccount CreateFakeBankAccount(Guid? userId = null, decimal balance = 10_000m)
        {
            return new FakeBankAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                AccountNumber = $"FBA{_faker.Random.Number(10000000, 99999999)}",
                Balance = balance,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Notification CreateNotification(Guid? userId = null, bool isRead = false)
        {
            return new Faker<Notification>()
                .RuleFor(n => n.Id, f => f.Random.Guid())
                .RuleFor(n => n.UserId, _ => userId ?? Guid.NewGuid())
                .RuleFor(n => n.Title, f => f.Lorem.Sentence(3))
                .RuleFor(n => n.Body, f => f.Lorem.Sentence(8))
                .RuleFor(n => n.Type, _ => NotificationType.Transaction)
                .RuleFor(n => n.IsRead, _ => isRead)
                .RuleFor(n => n.CreatedAt, f => f.Date.Recent(7))
                .Generate();
        }

        public static ExchangeRate CreateExchangeRate(
            string from = "USD", string to = "EGP", decimal rate = 30.75m)
        {
            return new ExchangeRate
            {
                Id = Guid.NewGuid(),
                FromCurrency = from,
                ToCurrency = to,
                Rate = rate,
                LastUpdated = DateTime.UtcNow,
                Source = "ExchangeRate-API",
                IsActive = true
            };
        }

        // ── DTOs ──────────────────────────────────────────────

        public static RegisterRequestDto CreateRegisterRequest(
            Action<RegisterRequestDto>? configure = null)
        {
            var dto = new RegisterRequestDto
            {
                FullName = _faker.Name.FullName(),
                Email = _faker.Internet.Email(),
                PhoneNumber = $"010{_faker.Random.Number(10000000, 99999999)}",
                Password = "Test@1234",
                ConfirmPassword = "Test@1234"
            };
            configure?.Invoke(dto);
            return dto;
        }

        public static LoginRequestDto CreateLoginRequest(
            string? emailOrPhone = null, string? password = null) => new()
            {
                EmailOrPhone = emailOrPhone ?? _faker.Internet.Email(),
                Password = password ?? "Test@1234"
            };

        // ✅ FIX: Correct property names from actual SendMoneyRequestDto
        public static SendMoneyRequestDto CreateSendMoneyRequest(
            Guid? senderWalletId = null, decimal amount = 500m, string? otpCode = null)
        {
            return new SendMoneyRequestDto
            {
                SenderWalletId = senderWalletId ?? Guid.NewGuid(),
                ReceiverPhoneOrEmail = _faker.Internet.Email(),
                Amount = amount,
                Description = _faker.Lorem.Sentence(5),
                OtpCode = otpCode ?? "123456"
            };
        }

        public static PayBillRequestDto CreatePayBillRequest(
            Guid? walletId = null, Guid? billerId = null,
            decimal amount = 350m, string? otpCode = null)
        {
            return new PayBillRequestDto
            {
                WalletId = walletId ?? Guid.NewGuid(),
                BillerId = billerId ?? Guid.NewGuid(),
                Amount = amount,
                OtpCode = otpCode ?? "123456"
            };
        }

        // ── Collections ────────────────────────────────────────

        public static List<User> CreateUsers(int n = 5) => Enumerable.Range(0, n).Select(_ => CreateUser()).ToList();
        public static List<Transaction> CreateTransactions(Guid wid, int n) => Enumerable.Range(0, n).Select(_ => CreateTransaction(wid)).ToList();
        public static List<Notification> CreateNotifications(Guid uid, int n) => Enumerable.Range(0, n).Select(_ => CreateNotification(uid)).ToList();
        public static List<Biller> CreateBillers(int n = 12) => Enumerable.Range(0, n).Select(_ => CreateBiller()).ToList();
    }
}