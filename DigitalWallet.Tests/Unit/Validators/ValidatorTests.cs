using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.DTOs.Transfer;
using DigitalWallet.Application.Validators;
using DigitalWallet.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DigitalWallet.Tests.Unit.Validators
{
    // ════════════════════════════════════════════════════════
    // REGISTER
    // ════════════════════════════════════════════════════════
    public class RegisterRequestValidatorTests
    {
        private readonly RegisterRequestValidator _sut = new();

        [Fact]
        public async Task ValidRequest_PassesAllRules()
            => (await _sut.ValidateAsync(TestDataBuilder.CreateRegisterRequest())).IsValid.Should().BeTrue();

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task EmptyFullName_Fails(string? name)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r => r.FullName = name!);
            var res = await _sut.ValidateAsync(req);
            res.IsValid.Should().BeFalse();
            res.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequestDto.FullName));
        }

        [Theory]
        [InlineData("invalidemail")]
        [InlineData("@nodomain.com")]
        [InlineData("")]
        public async Task InvalidEmail_Fails(string email)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r => r.Email = email);
            var res = await _sut.ValidateAsync(req);
            res.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequestDto.Email));
        }

        [Theory]
        [InlineData("test@gmail.com")]
        [InlineData("user.name@domain.org")]
        public async Task ValidEmail_Passes(string email)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r => r.Email = email);
            var res = await _sut.ValidateAsync(req);
            res.Errors.Should().NotContain(e => e.PropertyName == nameof(RegisterRequestDto.Email));
        }

        [Theory]
        [InlineData("short")]
        [InlineData("nouppercase1@")]
        [InlineData("NOLOWERCASE1@")]
        [InlineData("NoSpecialChar1")]
        [InlineData("NoNumbers@Abc")]
        public async Task WeakPassword_Fails(string password)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r =>
            {
                r.Password = password;
                r.ConfirmPassword = password;
            });
            (await _sut.ValidateAsync(req)).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("Test@1234")]
        [InlineData("Secure@Pass99")]
        public async Task StrongPassword_Passes(string password)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r =>
            {
                r.Password = password;
                r.ConfirmPassword = password;
            });
            (await _sut.ValidateAsync(req)).Errors
                .Should().NotContain(e => e.PropertyName == nameof(RegisterRequestDto.Password));
        }

        [Fact]
        public async Task MismatchedPasswords_Fails()
        {
            var req = TestDataBuilder.CreateRegisterRequest(r =>
            {
                r.Password = "Test@1234";
                r.ConfirmPassword = "Different@9999";
            });
            (await _sut.ValidateAsync(req)).Errors
                .Should().Contain(e => e.PropertyName == nameof(RegisterRequestDto.ConfirmPassword));
        }

        [Theory]
        [InlineData("01012345678")]
        [InlineData("01112345678")]
        [InlineData("01212345678")]
        public async Task ValidEgyptianPhone_Passes(string phone)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r => r.PhoneNumber = phone);
            (await _sut.ValidateAsync(req)).Errors
                .Should().NotContain(e => e.PropertyName == nameof(RegisterRequestDto.PhoneNumber));
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("02012345678")]
        [InlineData("abc12345678")]
        public async Task InvalidPhone_Fails(string phone)
        {
            var req = TestDataBuilder.CreateRegisterRequest(r => r.PhoneNumber = phone);
            (await _sut.ValidateAsync(req)).IsValid.Should().BeFalse();
        }
    }

    // ════════════════════════════════════════════════════════
    // LOGIN
    // ════════════════════════════════════════════════════════
    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator _sut = new();

        [Fact]
        public async Task ValidEmailLogin_Passes()
            => (await _sut.ValidateAsync(TestDataBuilder.CreateLoginRequest("test@example.com", "Test@1234")))
                .IsValid.Should().BeTrue();

        [Fact]
        public async Task ValidPhoneLogin_Passes()
            => (await _sut.ValidateAsync(new LoginRequestDto { EmailOrPhone = "01012345678", Password = "Test@1234" }))
                .IsValid.Should().BeTrue();

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EmptyEmailOrPhone_Fails(string value)
        {
            var res = await _sut.ValidateAsync(new LoginRequestDto { EmailOrPhone = value, Password = "Test@1234" });
            res.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequestDto.EmailOrPhone));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EmptyPassword_Fails(string password)
        {
            var res = await _sut.ValidateAsync(new LoginRequestDto { EmailOrPhone = "test@example.com", Password = password });
            res.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequestDto.Password));
        }
    }

    // ════════════════════════════════════════════════════════
    // SEND MONEY
    // ════════════════════════════════════════════════════════
    public class SendMoneyRequestValidatorTests
    {
        private readonly SendMoneyRequestValidator _sut = new();

        [Fact]
        public async Task ValidRequest_Passes()
            => (await _sut.ValidateAsync(TestDataBuilder.CreateSendMoneyRequest()))
                .IsValid.Should().BeTrue();

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task ZeroOrNegativeAmount_Fails(decimal amount)
        {
            var res = await _sut.ValidateAsync(TestDataBuilder.CreateSendMoneyRequest(amount: amount));
            // ✅ FIX: Use actual DTO property name
            res.Errors.Should().Contain(e => e.PropertyName == nameof(SendMoneyRequestDto.Amount));
        }

        [Fact]
        public async Task EmptySenderWalletId_Fails()
        {
            // ✅ FIX: SenderWalletId is the real property name
            var res = await _sut.ValidateAsync(TestDataBuilder.CreateSendMoneyRequest(senderWalletId: Guid.Empty));
            res.Errors.Should().Contain(e => e.PropertyName == nameof(SendMoneyRequestDto.SenderWalletId));
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("1234567")]
        [InlineData("abcdef")]
        [InlineData("")]
        public async Task InvalidOtpFormat_Fails(string otp)
        {
            var res = await _sut.ValidateAsync(TestDataBuilder.CreateSendMoneyRequest(otpCode: otp));
            res.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(500)]
        [InlineData(10_000)]
        public async Task ValidAmounts_Pass(decimal amount)
        {
            var res = await _sut.ValidateAsync(TestDataBuilder.CreateSendMoneyRequest(amount: amount));
            res.Errors.Should().NotContain(e => e.PropertyName == nameof(SendMoneyRequestDto.Amount));
        }
    }
}