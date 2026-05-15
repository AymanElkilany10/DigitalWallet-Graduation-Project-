using DigitalWallet.Application.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Tests.Fixtures;
using FluentAssertions;
using Moq;
using Xunit;

namespace DigitalWallet.Tests.Unit.Services
{
    public class BillPaymentServiceTests : ServiceTestBase
    {
        private readonly BillPaymentService _sut;

        public BillPaymentServiceTests()
        {
            _sut = new BillPaymentService(
                UnitOfWorkMock.Object,
                RealMapper,
                NotificationServiceMock.Object,
                CachingServiceMock.Object
            );
        }

        // ════════════════════════════════════════════════════
        // GET BILLERS
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task GetBillers_ReturnsAllActiveBillers()
        {
            var billers = TestDataBuilder.CreateBillers(12);
            BillerRepositoryMock.Setup(r => r.GetActiveBillersAsync()).ReturnsAsync(billers);

            var result = await _sut.GetAllBillersAsync();

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(12);
        }

        [Fact]
        public async Task GetBillers_WhenNoBillers_ReturnsEmptyList()
        {
            BillerRepositoryMock.Setup(r => r.GetActiveBillersAsync())
                .ReturnsAsync(new List<Biller>());

            var result = await _sut.GetAllBillersAsync();

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        // ════════════════════════════════════════════════════
        // PAY BILL — SUCCESS
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task PayBill_ValidRequest_ReturnsSuccess()
        {
            var (u, w, b, otp) = Scene();
            SetupMocks(u.Id, w, b, otp);

            var result = await _sut.PayBillAsync(u.Id,
                TestDataBuilder.CreatePayBillRequest(w.Id, b.Id, 350m));

            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Amount.Should().Be(350m);
        }

        [Fact]
        public async Task PayBill_ValidRequest_DeductsExactAmount()
        {
            var (u, w, b, otp) = Scene(balance: 5_000m);
            Wallet? saved = null;
            SetupMocks(u.Id, w, b, otp);

            // ✅ FIX: UpdateAsync returns Task (plain), use Callback to capture
            WalletRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Wallet>()))
                .Callback<Wallet>(x => saved = x)
                .Returns(Task.CompletedTask);

            await _sut.PayBillAsync(u.Id,
                TestDataBuilder.CreatePayBillRequest(w.Id, b.Id, 350m));

            saved!.Balance.Should().Be(4_650m);
        }

        [Fact]
        public async Task PayBill_ValidRequest_CreatesNegativeTransactionRecord()
        {
            var (u, w, b, otp) = Scene();
            SetupMocks(u.Id, w, b, otp);

            await _sut.PayBillAsync(u.Id,
                TestDataBuilder.CreatePayBillRequest(w.Id, b.Id, 350m));

            TransactionRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Transaction>(t =>
                    t.Type == TransactionType.Bill &&
                    t.Amount == -350m)),
                Times.Once);
        }

        [Fact]
        public async Task PayBill_ValidRequest_MarksOtpUsed()
        {
            var (u, w, b, otp) = Scene();
            SetupMocks(u.Id, w, b, otp);

            await _sut.PayBillAsync(u.Id, TestDataBuilder.CreatePayBillRequest(w.Id, b.Id));

            OtpCodeRepositoryMock.Verify(r => r.MarkAsUsedAsync(otp.Id), Times.Once);
        }

        [Fact]
        public async Task PayBill_ValidRequest_CommitsTransaction()
        {
            var (u, w, b, otp) = Scene();
            SetupMocks(u.Id, w, b, otp);

            await _sut.PayBillAsync(u.Id, TestDataBuilder.CreatePayBillRequest(w.Id, b.Id));

            UnitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
            UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task PayBill_ValidRequest_CreatesNotification()
        {
            var (u, w, b, otp) = Scene();
            SetupMocks(u.Id, w, b, otp);

            await _sut.PayBillAsync(u.Id, TestDataBuilder.CreatePayBillRequest(w.Id, b.Id));

            NotificationRepositoryMock.Verify(r =>
                r.AddAsync(It.IsAny<Notification>()), Times.Once);
        }

        // ════════════════════════════════════════════════════
        // PAY BILL — FAILURES
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task PayBill_InvalidOtp_Fails_NoBalanceChange()
        {
            var userId = Guid.NewGuid();
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(userId, It.IsAny<string>(), OtpType.Transfer))
                .ReturnsAsync((OtpCode?)null);

            var result = await _sut.PayBillAsync(userId,
                TestDataBuilder.CreatePayBillRequest());

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("invalid or expired otp");
            WalletRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Wallet>()), Times.Never);
        }

        [Fact]
        public async Task PayBill_WalletBelongsToOtherUser_Fails()
        {
            var user = TestDataBuilder.CreateUser();
            var other = TestDataBuilder.CreateUser();
            var wallet = TestDataBuilder.CreateWallet(other.Id, balance: 5_000m);
            var otp = TestDataBuilder.CreateOtpCode(user.Id, type: OtpType.Transfer);

            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(user.Id, "123456", OtpType.Transfer)).ReturnsAsync(otp);
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(wallet.Id)).ReturnsAsync(wallet);

            var result = await _sut.PayBillAsync(user.Id,
                TestDataBuilder.CreatePayBillRequest(wallet.Id));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("wallet not found");
        }

        [Fact]
        public async Task PayBill_InsufficientBalance_Fails_RollsBack()
        {
            var (u, w, b, otp) = Scene(balance: 100m);
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(u.Id, "123456", OtpType.Transfer)).ReturnsAsync(otp);
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(w.Id)).ReturnsAsync(w);

            var result = await _sut.PayBillAsync(u.Id,
                TestDataBuilder.CreatePayBillRequest(w.Id, b.Id, 350m));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("insufficient balance");
            UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task PayBill_InactiveBiller_Fails()
        {
            var (u, w, _, otp) = Scene();
            var inactive = TestDataBuilder.CreateBiller(isActive: false);

            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(u.Id, "123456", OtpType.Transfer)).ReturnsAsync(otp);
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(w.Id)).ReturnsAsync(w);
            BillerRepositoryMock.Setup(r => r.GetByIdAsync(inactive.Id)).ReturnsAsync(inactive);

            var result = await _sut.PayBillAsync(u.Id,
                TestDataBuilder.CreatePayBillRequest(w.Id, inactive.Id));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("not available");
        }

        [Fact]
        public async Task PayBill_DbExceptionOnCommit_RollsBack()
        {
            var (u, w, b, otp) = Scene();
            SetupMocks(u.Id, w, b, otp);
            UnitOfWorkMock.Setup(x => x.CommitTransactionAsync())
                .ThrowsAsync(new Exception("timeout"));

            var result = await _sut.PayBillAsync(u.Id,
                TestDataBuilder.CreatePayBillRequest(w.Id, b.Id));

            result.IsSuccess.Should().BeFalse();
            UnitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        // ════════════════════════════════════════════════════
        // GET PAYMENT HISTORY
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task GetPaymentHistory_ReturnsPaginatedItems()
        {
            var userId = Guid.NewGuid();
            var payments = Enumerable.Range(0, 5).Select(_ => new BillPayment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = 350m,
                Status = TransactionStatus.Success
            }).ToList();

            BillPaymentRepositoryMock.Setup(r =>
                r.GetByUserIdAsync(userId, 1, 20)).ReturnsAsync(payments);
            SetupCacheAlwaysMiss();

            var result = await _sut.GetPaymentHistoryAsync(userId, 1, 20);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(5);
        }

        // ════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════

        private (User u, Wallet w, Biller b, OtpCode otp) Scene(decimal balance = 5_000m)
        {
            var u = TestDataBuilder.CreateUser();
            var w = TestDataBuilder.CreateWallet(u.Id, balance: balance);
            var b = TestDataBuilder.CreateBiller();
            var otp = TestDataBuilder.CreateOtpCode(u.Id, type: OtpType.Transfer);
            return (u, w, b, otp);
        }

        private void SetupMocks(Guid userId, Wallet wallet, Biller biller, OtpCode otp)
        {
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(userId, "123456", OtpType.Transfer)).ReturnsAsync(otp);
            WalletRepositoryMock.Setup(r => r.GetByIdAsync(wallet.Id)).ReturnsAsync(wallet);
            BillerRepositoryMock.Setup(r => r.GetByIdAsync(biller.Id)).ReturnsAsync(biller);

            // ✅ FIX: All AddAsync/UpdateAsync return Task (plain), NOT Task<T>
            WalletRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Wallet>()))
                .Returns(Task.CompletedTask);
            BillPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<BillPayment>()))
                .Returns((Task<BillPayment>)Task.CompletedTask);
            TransactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
                .Returns((Task<Transaction>)Task.CompletedTask);
            NotificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns((Task<Notification>)Task.CompletedTask);
            OtpCodeRepositoryMock.Setup(r => r.MarkAsUsedAsync(otp.Id))
                .Returns(Task.CompletedTask);
        }
    }
}