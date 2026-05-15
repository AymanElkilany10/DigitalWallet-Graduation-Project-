using DigitalWallet.Application.DTOs.Transfer;
using DigitalWallet.Application.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Tests.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace DigitalWallet.Tests.Unit.Services
{
    public class TransferServiceTests : ServiceTestBase
    {
        private readonly TransferService _sut;
        private static readonly Bogus.Faker _faker = new();

        public TransferServiceTests()
        {
            _sut = new TransferService(
                UnitOfWorkMock.Object,
                RealMapper,
                CachingServiceMock.Object
            );
        }

        // ════════════════════════════════════════════════════
        // SEND MONEY — SUCCESS
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task SendMoney_ValidRequest_ReturnsSuccess()
        {
            var s = BuildScene();
            SetupMocks(s);

            var result = await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            result.IsSuccess.Should().BeTrue();
            result.Data!.Amount.Should().Be(500m);
        }

        [Fact]
        public async Task SendMoney_ValidRequest_DeductsSenderBalance()
        {
            var s = BuildScene(senderBalance: 5_000m);
            Wallet? updated = null;
            SetupMocks(s);

            // ✅ FIX: UpdateAsync returns Task (plain), not Task<Wallet>
            WalletRepositoryMock
                .Setup(r => r.UpdateAsync(It.Is<Wallet>(w => w.Id == s.SenderWallet.Id)))
                .Callback<Wallet>(w => updated = w)
                .Returns(Task.CompletedTask);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id, 1_000m));

            updated!.Balance.Should().Be(4_000m);
        }

        [Fact]
        public async Task SendMoney_ValidRequest_CreditsReceiverBalance()
        {
            var s = BuildScene(senderBalance: 5_000m, receiverBalance: 0m);
            Wallet? updated = null;
            SetupMocks(s);

            WalletRepositoryMock
                .Setup(r => r.UpdateAsync(It.Is<Wallet>(w => w.Id == s.ReceiverWallet.Id)))
                .Callback<Wallet>(w => updated = w)
                .Returns(Task.CompletedTask);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id, 1_000m));

            updated!.Balance.Should().Be(1_000m);
        }

        [Fact]
        public async Task SendMoney_ValidRequest_CreatesTwoTransactionRecords()
        {
            var s = BuildScene();
            SetupMocks(s);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            TransactionRepositoryMock.Verify(r =>
                r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2),
                "one debit for sender + one credit for receiver");
        }

        [Fact]
        public async Task SendMoney_ValidRequest_CreatesTransferRecord()
        {
            var s = BuildScene();
            SetupMocks(s);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            TransferRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Transfer>()), Times.Once);
        }

        [Fact]
        public async Task SendMoney_ValidRequest_MarksOtpUsed()
        {
            var s = BuildScene();
            SetupMocks(s);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            OtpCodeRepositoryMock.Verify(r => r.MarkAsUsedAsync(s.Otp.Id), Times.Once,
                "OTP must be consumed to prevent replay attacks");
        }

        [Fact]
        public async Task SendMoney_ValidRequest_CommitsTransaction()
        {
            var s = BuildScene();
            SetupMocks(s);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            UnitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task SendMoney_ValidRequest_CreatesTwoNotifications()
        {
            var s = BuildScene();
            SetupMocks(s);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            NotificationRepositoryMock.Verify(r =>
                r.AddAsync(It.IsAny<Notification>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SendMoney_ValidRequest_InvalidatesCaches()
        {
            var s = BuildScene();
            SetupMocks(s);

            await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            CachingServiceMock.Verify(c =>
                c.RemoveAsync(It.IsAny<string>()), Times.AtLeast(2));
        }

        // ════════════════════════════════════════════════════
        // SEND MONEY — FAILURES
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task SendMoney_WalletNotFound_Fails()
        {
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Wallet?)null);

            var result = await _sut.SendMoneyAsync(MakeRequest(Guid.NewGuid()));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("sender wallet not found");
        }

        [Fact]
        public async Task SendMoney_InvalidOtp_Fails_NoBalanceChanges()
        {
            var s = BuildScene();
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(s.SenderWallet.Id))
                .ReturnsAsync(s.SenderWallet);
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(s.Sender.Id, "999999", OtpType.Transfer))
                .ReturnsAsync((OtpCode?)null);

            var result = await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id, otpCode: "999999"));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("invalid or expired otp");
            WalletRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Wallet>()), Times.Never);
        }

        [Fact]
        public async Task SendMoney_ReceiverNotFound_Fails()
        {
            var s = BuildScene();
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(s.SenderWallet.Id))
                .ReturnsAsync(s.SenderWallet);
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(s.Sender.Id, "123456", OtpType.Transfer))
                .ReturnsAsync(s.Otp);
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            UserRepositoryMock.Setup(r => r.GetByPhoneNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var result = await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("receiver not found");
        }

        [Fact]
        public async Task SendMoney_ReceiverHasNoCurrencyWallet_Fails()
        {
            var s = BuildScene();
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(s.SenderWallet.Id))
                .ReturnsAsync(s.SenderWallet);
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(s.Sender.Id, "123456", OtpType.Transfer))
                .ReturnsAsync(s.Otp);
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(s.Receiver);
            WalletRepositoryMock.Setup(r =>
                r.GetByUserIdAndCurrencyAsync(s.Receiver.Id, s.SenderWallet.CurrencyCode))
                .ReturnsAsync((Wallet?)null);

            var result = await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain(s.SenderWallet.CurrencyCode);
        }

        [Fact]
        public async Task SendMoney_InsufficientBalance_Fails_RollsBack()
        {
            var s = BuildScene(senderBalance: 100m);
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(s.SenderWallet.Id))
                .ReturnsAsync(s.SenderWallet);
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(s.Sender.Id, "123456", OtpType.Transfer))
                .ReturnsAsync(s.Otp);
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(s.Receiver);
            WalletRepositoryMock.Setup(r =>
                r.GetByUserIdAndCurrencyAsync(s.Receiver.Id, s.SenderWallet.CurrencyCode))
                .ReturnsAsync(s.ReceiverWallet);

            var result = await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id, 1_000m));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("insufficient balance");
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task SendMoney_DbExceptionOnCommit_RollsBack()
        {
            var s = BuildScene();
            SetupMocks(s);
            UnitOfWorkMock.Setup(u => u.CommitTransactionAsync())
                .ThrowsAsync(new Exception("connection lost"));

            var result = await _sut.SendMoneyAsync(MakeRequest(s.SenderWallet.Id));

            result.IsSuccess.Should().BeFalse();
            UnitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        // ════════════════════════════════════════════════════
        // GET TRANSFER HISTORY
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task GetTransferHistory_ReturnsCombinedSentAndReceived()
        {
            var walletId = Guid.NewGuid();
            var sent = Enumerable.Range(0, 3).Select(_ => TestDataBuilder.CreateTransfer(walletId)).ToList();
            var received = Enumerable.Range(0, 2).Select(_ => TestDataBuilder.CreateTransfer(receiverWalletId: walletId)).ToList();

            TransferRepositoryMock.Setup(r =>
                r.GetBySenderWalletIdAsync(walletId, 1, 20)).ReturnsAsync(sent);
            TransferRepositoryMock.Setup(r =>
                r.GetByReceiverWalletIdAsync(walletId, 1, 20)).ReturnsAsync(received);
            SetupCacheAlwaysMiss();

            var result = await _sut.GetTransferHistoryAsync(walletId, 1, 20);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Count.Should().BeLessOrEqualTo(20);
        }

        [Fact]
        public async Task GetTransferHistory_EmptyHistory_ReturnsEmptyList()
        {
            var walletId = Guid.NewGuid();
            TransferRepositoryMock.Setup(r =>
                r.GetBySenderWalletIdAsync(walletId, 1, 20))
                .ReturnsAsync(new List<Transfer>());
            TransferRepositoryMock.Setup(r =>
                r.GetByReceiverWalletIdAsync(walletId, 1, 20))
                .ReturnsAsync(new List<Transfer>());
            SetupCacheAlwaysMiss();

            var result = await _sut.GetTransferHistoryAsync(walletId, 1, 20);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
        }

        // ════════════════════════════════════════════════════
        // SCENE + MOCK HELPERS
        // ════════════════════════════════════════════════════

        private record Scene(User Sender, User Receiver,
            Wallet SenderWallet, Wallet ReceiverWallet, OtpCode Otp);

        private Scene BuildScene(
            decimal senderBalance = 5_000m,
            decimal receiverBalance = 0m,
            string currency = "EGP")
        {
            var sender = TestDataBuilder.CreateUser();
            var receiver = TestDataBuilder.CreateUser();
            var sw = TestDataBuilder.CreateWallet(sender.Id, currency, senderBalance);
            var rw = TestDataBuilder.CreateWallet(receiver.Id, currency, receiverBalance);
            var otp = TestDataBuilder.CreateOtpCode(sender.Id, type: OtpType.Transfer);
            return new Scene(sender, receiver, sw, rw, otp);
        }

        private SendMoneyRequestDto MakeRequest(
            Guid walletId, decimal amount = 500m,
            string? otpCode = null)
        {
            return new SendMoneyRequestDto
            {
                SenderWalletId = walletId,
                ReceiverPhoneOrEmail = _faker.Internet.Email(),
                Amount = amount,
                Description = "Test transfer",
                OtpCode = otpCode ?? "123456"
            };
        }

        private void SetupMocks(Scene s)
        {
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(s.SenderWallet.Id))
                .ReturnsAsync(s.SenderWallet);

            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(s.Sender.Id, "123456", OtpType.Transfer))
                .ReturnsAsync(s.Otp);

            UserRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(s.Receiver);
            UserRepositoryMock.Setup(r => r.GetByPhoneNumberAsync(It.IsAny<string>()))
                .ReturnsAsync(s.Receiver);
            WalletRepositoryMock.Setup(r =>
                r.GetByUserIdAndCurrencyAsync(s.Receiver.Id, s.SenderWallet.CurrencyCode))
                .ReturnsAsync(s.ReceiverWallet);

            // ✅ FIX: All UpdateAsync/AddAsync return Task (plain), not Task<T>
            WalletRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Wallet>()))
                .Returns(Task.CompletedTask);
            TransferRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transfer>()))
                .Returns((Task<Transfer>)Task.CompletedTask);
            TransactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .Returns((Task<Transaction>)Task.CompletedTask);
            NotificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns((Task<Notification>)Task.CompletedTask);
            OtpCodeRepositoryMock.Setup(r => r.MarkAsUsedAsync(s.Otp.Id))
                .Returns(Task.CompletedTask);
        }
    }
}