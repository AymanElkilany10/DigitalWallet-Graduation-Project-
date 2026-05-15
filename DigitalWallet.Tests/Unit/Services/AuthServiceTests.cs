using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Services;
using DigitalWallet.Application.Settings;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DigitalWallet.Tests.Unit.Services
{
    public class AuthServiceTests : ServiceTestBase
    {
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            var jwtGenerator = new JwtTokenGenerator(
                "ThisIsAVerySecretKeyForTestingAtLeast32Chars!",
                "DigitalWallet.API",
                "DigitalWallet.Clients",
                24
            );

            // Mock ICachingService and IOptions<NotificationSettings> to satisfy the constructor
            var cachingServiceMock = new Mock<ICachingService>();
            var notificationSettings = new NotificationSettings();
            var notificationSettingsOptions = Options.Create(notificationSettings);

            _sut = new AuthService(
                UnitOfWorkMock.Object,
                RealMapper,
                jwtGenerator,
                SmsServiceMock.Object,
                EmailServiceMock.Object,
                cachingServiceMock.Object,
                notificationSettingsOptions
            );
        }

        // ════════════════════════════════════════════════════
        // REGISTER
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task Register_ValidData_ReturnsSuccessWithToken()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            ArrangeNewUser(req.Email, req.PhoneNumber);

            var result = await _sut.RegisterAsync(req);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Token.Should().NotBeNullOrWhiteSpace();
            result.Data.RequiresOtp.Should().BeFalse();
            result.Data.Email.Should().Be(req.Email);
        }

        [Fact]
        public async Task Register_ValidData_CreatesDefaultEgpWallet()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            ArrangeNewUser(req.Email, req.PhoneNumber);

            await _sut.RegisterAsync(req);

            WalletRepositoryMock.Verify(r => r.AddAsync(
                It.Is<Wallet>(w =>
                    w.CurrencyCode == "EGP" &&
                    w.Balance == 0 &&
                    w.DailyLimit == 5_000 &&
                    w.MonthlyLimit == 20_000)),
                Times.Once);
        }

        [Fact]
        public async Task Register_ValidData_CreatesFakeBankWith10000()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            ArrangeNewUser(req.Email, req.PhoneNumber);

            await _sut.RegisterAsync(req);

            FakeBankAccountRepositoryMock.Verify(r =>
                r.AddAsync(It.Is<FakeBankAccount>(b => b.Balance == 10_000)),
                Times.Once);
        }

        [Fact]
        public async Task Register_ValidData_PasswordIsHashed_NeverPlainText()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            User? saved = null;
            ArrangeNewUser(req.Email, req.PhoneNumber, captureUser: u => saved = u);

            await _sut.RegisterAsync(req);

            saved.Should().NotBeNull();
            saved!.PasswordHash.Should().NotBe(req.Password, "plain-text password must never be stored");
            saved.Salt.Should().NotBeNullOrEmpty();
            PasswordHasher.VerifyPassword(req.Password, saved.Salt, saved.PasswordHash)
                .Should().BeTrue("the hash must be verifiable with the same password");
        }

        [Fact]
        public async Task Register_ValidData_NewUserIsKycBasicAndActive()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            User? saved = null;
            ArrangeNewUser(req.Email, req.PhoneNumber, captureUser: u => saved = u);

            await _sut.RegisterAsync(req);

            saved!.KycLevel.Should().Be(KycLevel.Basic);
            saved.Status.Should().Be(UserStatus.Active);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Fails_NoUserCreated()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            UserRepositoryMock.Setup(r => r.EmailExistsAsync(req.Email)).ReturnsAsync(true);

            var result = await _sut.RegisterAsync(req);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            UserRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Register_DuplicatePhone_Fails()
        {
            var req = TestDataBuilder.CreateRegisterRequest();
            UserRepositoryMock.Setup(r => r.EmailExistsAsync(req.Email)).ReturnsAsync(false);
            UserRepositoryMock.Setup(r => r.PhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(true);

            var result = await _sut.RegisterAsync(req);

            result.IsSuccess.Should().BeFalse();
            UserRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        // ════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task Login_ValidCredentials_RequiresOtp_NoTokenYet()
        {
            var (user, password) = MakeHashedUser();
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            // ✅ FIX: AddAsync returns Task (not Task<T>)
            OtpCodeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OtpCode>()))
                .Returns((Task<OtpCode>)Task.CompletedTask);
            SetupCacheAlwaysMiss();

            var result = await _sut.LoginAsync(TestDataBuilder.CreateLoginRequest(user.Email, password));

            result.IsSuccess.Should().BeTrue();
            result.Data!.RequiresOtp.Should().BeTrue();
            result.Data.Token.Should().BeNullOrEmpty("JWT is not issued until OTP is verified");
            result.Data.UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task Login_ValidCredentials_SavesOtpToDatabase()
        {
            var (user, password) = MakeHashedUser();
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            OtpCodeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OtpCode>()))
                .Returns((Task<OtpCode>)Task.CompletedTask);
            SetupCacheAlwaysMiss();

            await _sut.LoginAsync(TestDataBuilder.CreateLoginRequest(user.Email, password));

            OtpCodeRepositoryMock.Verify(r => r.AddAsync(
                It.Is<OtpCode>(o =>
                    o.UserId == user.Id &&
                    o.Type == OtpType.Login &&
                    !o.IsUsed &&
                    o.ExpiresAt > DateTime.UtcNow)),
                Times.Once);
        }

        [Fact]
        public async Task Login_ValidCredentials_SendsOtpEmail()
        {
            var (user, password) = MakeHashedUser();
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            OtpCodeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OtpCode>()))
                .Returns((Task<OtpCode>)Task.CompletedTask);
            SetupCacheAlwaysMiss();

            await _sut.LoginAsync(TestDataBuilder.CreateLoginRequest(user.Email, password));

            EmailServiceMock.Verify(
                e => e.SendOtpEmailAsync(user.Email, It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Login_WrongPassword_Fails_NeverSendsOtp()
        {
            var (user, _) = MakeHashedUser();
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            SetupCacheAlwaysMiss();

            var result = await _sut.LoginAsync(
                TestDataBuilder.CreateLoginRequest(user.Email, "WrongPass@999"));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            OtpCodeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OtpCode>()), Times.Never);
            EmailServiceMock.Verify(e =>
                e.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_UserNotFound_Fails()
        {
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            SetupCacheAlwaysMiss();

            var result = await _sut.LoginAsync(TestDataBuilder.CreateLoginRequest());

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Login_SuspendedAccount_Fails()
        {
            var (user, password) = MakeHashedUser(UserStatus.Suspended);
            UserRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            SetupCacheAlwaysMiss();

            var result = await _sut.LoginAsync(
                TestDataBuilder.CreateLoginRequest(user.Email, password));

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("suspend");
        }

        [Fact]
        public async Task Login_WithPhoneNumber_QueriesUserByPhone_NotEmail()
        {
            var (user, password) = MakeHashedUser();
            UserRepositoryMock.Setup(r => r.GetByPhoneNumberAsync(user.PhoneNumber))
                .ReturnsAsync(user);
            OtpCodeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OtpCode>()))
                .Returns((Task<OtpCode>)Task.CompletedTask);
            SetupCacheAlwaysMiss();

            await _sut.LoginAsync(new LoginRequestDto
            {
                EmailOrPhone = user.PhoneNumber,
                Password = password
            });

            UserRepositoryMock.Verify(r => r.GetByPhoneNumberAsync(user.PhoneNumber), Times.Once);
            UserRepositoryMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        // ════════════════════════════════════════════════════
        // VERIFY OTP
        // ════════════════════════════════════════════════════

        [Fact]
        public async Task VerifyOtp_ValidCode_ReturnsJwtToken()
        {
            var user = TestDataBuilder.CreateUser();
            var otp = TestDataBuilder.CreateOtpCode(user.Id);
            ArrangeVerifyOtp(user, otp);

            var result = await _sut.VerifyOtpAsync(
                new VerifyOtpRequestDto { UserId = user.Id, Code = "123456" });

            result.IsSuccess.Should().BeTrue();
            result.Data!.Token.Should().NotBeNullOrWhiteSpace();
            result.Data.RequiresOtp.Should().BeFalse();
        }

        [Fact]
        public async Task VerifyOtp_ValidCode_MarksOtpAsUsed()
        {
            var user = TestDataBuilder.CreateUser();
            var otp = TestDataBuilder.CreateOtpCode(user.Id);
            ArrangeVerifyOtp(user, otp);

            await _sut.VerifyOtpAsync(new VerifyOtpRequestDto { UserId = user.Id, Code = "123456" });

            OtpCodeRepositoryMock.Verify(r => r.MarkAsUsedAsync(otp.Id), Times.Once,
                "OTP must be marked used to prevent replay attacks");
        }

        [Fact]
        public async Task VerifyOtp_ValidCode_UpdatesLastLoginAt()
        {
            var user = TestDataBuilder.CreateUser();
            var otp = TestDataBuilder.CreateOtpCode(user.Id);
            User? saved = null;
            ArrangeVerifyOtp(user, otp, captureUpdate: u => saved = u);

            await _sut.VerifyOtpAsync(new VerifyOtpRequestDto { UserId = user.Id, Code = "123456" });

            saved!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task VerifyOtp_ValidCode_InvalidatesUserCaches()
        {
            var user = TestDataBuilder.CreateUser();
            var otp = TestDataBuilder.CreateOtpCode(user.Id);
            ArrangeVerifyOtp(user, otp);

            await _sut.VerifyOtpAsync(new VerifyOtpRequestDto { UserId = user.Id, Code = "123456" });

            CachingServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task VerifyOtp_WrongCode_Fails_NoTokenIssued()
        {
            var userId = Guid.NewGuid();
            OtpCodeRepositoryMock.Setup(r => r.GetValidOtpAsync(userId, "000000", OtpType.Login))
                .ReturnsAsync((OtpCode?)null);

            var result = await _sut.VerifyOtpAsync(
                new VerifyOtpRequestDto { UserId = userId, Code = "000000" });

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().ContainEquivalentOf("invalid or expired otp");
            UserRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task VerifyOtp_ExpiredOtp_Fails()
        {
            var userId = Guid.NewGuid();
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(userId, It.IsAny<string>(), OtpType.Login))
                .ReturnsAsync((OtpCode?)null);

            var result = await _sut.VerifyOtpAsync(
                new VerifyOtpRequestDto { UserId = userId, Code = "123456" });

            result.IsSuccess.Should().BeFalse();
        }

        // ════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════

        private void ArrangeNewUser(string email, string phone, Action<User>? captureUser = null)
        {
            UserRepositoryMock.Setup(r => r.EmailExistsAsync(email)).ReturnsAsync(false);
            UserRepositoryMock.Setup(r => r.PhoneExistsAsync(phone)).ReturnsAsync(false);

            // ✅ FIX: AddAsync returns Task (plain), not Task<User>
            // Use Callback to capture the entity, then Returns(Task.CompletedTask)
            UserRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Callback<User>(u => captureUser?.Invoke(u))
                .Returns((Task<User>)Task.CompletedTask);

            WalletRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Wallet>()))
                .Returns((Task<Wallet>)Task.CompletedTask);

            FakeBankAccountRepositoryMock.Setup(r => r.AddAsync(It.IsAny<FakeBankAccount>()))
                .Returns((Task<FakeBankAccount>)Task.CompletedTask);
        }

        private (User user, string password) MakeHashedUser(UserStatus status = UserStatus.Active)
        {
            const string pw = "Test@1234";
            var salt = PasswordHasher.GenerateSalt();
            var user = TestDataBuilder.CreateUser(u =>
            {
                u.PasswordHash = PasswordHasher.HashPassword(pw, salt);
                u.Salt = salt;
                u.Status = status;
            });
            return (user, pw);
        }

        private void ArrangeVerifyOtp(User user, OtpCode otp, Action<User>? captureUpdate = null)
        {
            OtpCodeRepositoryMock.Setup(r =>
                r.GetValidOtpAsync(user.Id, "123456", OtpType.Login)).ReturnsAsync(otp);

            UserRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            OtpCodeRepositoryMock.Setup(r => r.MarkAsUsedAsync(otp.Id)).Returns(Task.CompletedTask);

            // ✅ FIX: UpdateAsync returns Task (plain), not Task<User>
            UserRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Callback<User>(u => captureUpdate?.Invoke(u))
                .Returns(Task.CompletedTask);
        }
    }
}