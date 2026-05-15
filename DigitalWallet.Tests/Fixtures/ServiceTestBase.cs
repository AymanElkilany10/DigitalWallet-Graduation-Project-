using AutoMapper;
using DigitalWallet.Application.ExternalServices.Email;
using DigitalWallet.Application.ExternalServices.SMS;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Mappings;
using DigitalWallet.Application.Settings;
using Moq;

namespace DigitalWallet.Tests.Fixtures
{
    public abstract class ServiceTestBase
    {
        protected readonly Mock<IUnitOfWork> UnitOfWorkMock = new();
        protected readonly Mock<ICachingService> CachingServiceMock = new();
        protected readonly Mock<INotificationService> NotificationServiceMock = new();
        protected readonly Mock<IEmailService> EmailServiceMock = new();
        protected readonly Mock<ISmsService> SmsServiceMock = new();

        protected readonly Mock<IUserRepository> UserRepositoryMock = new();
        protected readonly Mock<IWalletRepository> WalletRepositoryMock = new();
        protected readonly Mock<ITransactionRepository> TransactionRepositoryMock = new();
        protected readonly Mock<ITransferRepository> TransferRepositoryMock = new();
        protected readonly Mock<IBillPaymentRepository> BillPaymentRepositoryMock = new();
        protected readonly Mock<IBillerRepository> BillerRepositoryMock = new();
        protected readonly Mock<IOtpCodeRepository> OtpCodeRepositoryMock = new();
        protected readonly Mock<INotificationRepository> NotificationRepositoryMock = new();
        protected readonly Mock<IFakeBankAccountRepository> FakeBankAccountRepositoryMock = new();
        protected readonly Mock<IFakeBankTransactionRepository> FakeBankTransactionRepositoryMock = new();
        protected readonly Mock<IExchangeRateRepository> ExchangeRateRepositoryMock = new();
        protected readonly Mock<ICurrencyExchangeRepository> CurrencyExchangeRepositoryMock = new();

        protected readonly IMapper RealMapper;

        // ✅ FIX: Removed ShowOtpInResponse — it doesn't exist in your NotificationSettings class
        protected readonly NotificationSettings NotificationSettings = new()
        {
            SendOtpViaEmail = true,
            SendOtpViaSms = false,
            SendWelcomeEmail = true,
            SendWelcomeSms = false,
            SendTransactionAlerts = true,
            LargeTransactionThreshold = 5_000m
        };

        protected ServiceTestBase()
        {
            UnitOfWorkMock.Setup(u => u.Users).Returns(UserRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.Wallets).Returns(WalletRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.Transactions).Returns(TransactionRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.Transfers).Returns(TransferRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.BillPayments).Returns(BillPaymentRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.Billers).Returns(BillerRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.OtpCodes).Returns(OtpCodeRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.Notifications).Returns(NotificationRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.FakeBankAccounts).Returns(FakeBankAccountRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.FakeBankTransactions).Returns(FakeBankTransactionRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.ExchangeRates).Returns(ExchangeRateRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.CurrencyExchanges).Returns(CurrencyExchangeRepositoryMock.Object);

            UnitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            UnitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            EmailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            EmailServiceMock.Setup(e => e.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            EmailServiceMock.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            SmsServiceMock.Setup(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            SmsServiceMock.Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            CachingServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            CachingServiceMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
            RealMapper = cfg.CreateMapper();
        }

        protected void SetupCacheAlwaysMiss()
        {
            CachingServiceMock
                .Setup(c => c.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<object>>>(),
                    It.IsAny<TimeSpan?>()))
                .Returns<string, Func<Task<object>>, TimeSpan?>(
                    async (_, factory, _) => await factory());
        }
    }
}