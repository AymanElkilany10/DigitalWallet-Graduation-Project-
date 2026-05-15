using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.Settings;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Xunit;
using DigitalWallet.Application.Common.Models;

namespace DigitalWallet.Tests.Unit.Helpers
{
    // ════════════════════════════════════════════════════════════
    // PASSWORD HASHER
    // ════════════════════════════════════════════════════════════
    public class PasswordHasherTests
    {
        [Fact]
        public void GenerateSalt_ReturnsNonEmptyString()
            => PasswordHasher.GenerateSalt().Should().NotBeNullOrEmpty();

        [Fact]
        public void GenerateSalt_EachCallReturnsUniqueSalt()
        {
            var s1 = PasswordHasher.GenerateSalt();
            var s2 = PasswordHasher.GenerateSalt();
            s1.Should().NotBe(s2);
        }

        [Fact]
        public void HashPassword_NeverEqualsPlainText()
        {
            var salt = PasswordHasher.GenerateSalt();
            PasswordHasher.HashPassword("Test@1234", salt)
                .Should().NotBe("Test@1234");
        }

        [Fact]
        public void HashPassword_SameInputAlwaysSameHash()
        {
            var salt = PasswordHasher.GenerateSalt();
            PasswordHasher.HashPassword("Test@1234", salt)
                .Should().Be(PasswordHasher.HashPassword("Test@1234", salt));
        }

        [Fact]
        public void HashPassword_DifferentSaltsDifferentHashes()
        {
            var h1 = PasswordHasher.HashPassword("Test@1234", PasswordHasher.GenerateSalt());
            var h2 = PasswordHasher.HashPassword("Test@1234", PasswordHasher.GenerateSalt());
            h1.Should().NotBe(h2);
        }

        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword("Test@1234", salt);
            PasswordHasher.VerifyPassword("Test@1234", salt, hash).Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword("Test@1234", salt);
            PasswordHasher.VerifyPassword("Wrong@999", salt, hash).Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WrongSalt_ReturnsFalse()
        {
            var correct = PasswordHasher.GenerateSalt();
            var wrong = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword("Test@1234", correct);
            PasswordHasher.VerifyPassword("Test@1234", wrong, hash).Should().BeFalse();
        }

        [Theory]
        [InlineData("Simple1@")]
        [InlineData("VeryLong@Password123")]
        [InlineData("P@ssw0rd!")]
        public void VerifyPassword_RoundTripSucceeds(string password)
        {
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);
            PasswordHasher.VerifyPassword(password, salt, hash).Should().BeTrue();
        }
    }

    // ════════════════════════════════════════════════════════════
    // OTP GENERATOR
    // ════════════════════════════════════════════════════════════
    public class OtpGeneratorTests
    {
        [Fact]
        public void GenerateOtpCode_Default_Returns6Digits()
        {
            var otp = OtpGenerator.GenerateOtpCode();
            otp.Should().HaveLength(6);
            otp.Should().MatchRegex(@"^\d{6}$");
        }

        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(10)]
        public void GenerateOtpCode_CorrectLength(int length)
            => OtpGenerator.GenerateOtpCode(length).Should().HaveLength(length);

        [Theory]
        [InlineData(3)]
        [InlineData(11)]
        public void GenerateOtpCode_InvalidLength_Throws(int length)
            => Assert.Throws<ArgumentException>(() => OtpGenerator.GenerateOtpCode(length));

        [Fact]
        public void GenerateOtpCode_OnlyDigits()
        {
            Enumerable.Range(0, 50).Select(_ => OtpGenerator.GenerateOtpCode())
                .Should().AllSatisfy(c => c.Should().MatchRegex(@"^\d+$"));
        }

        [Fact]
        public void GenerateOtpCode_ProducesVariedCodes()
        {
            Enumerable.Range(0, 50).Select(_ => OtpGenerator.GenerateOtpCode())
                .Distinct().Count().Should().BeGreaterThan(1);
        }

        [Fact]
        public void GenerateAccountNumber_StartsWith_FBA()
            => OtpGenerator.GenerateAccountNumber().Should().StartWith("FBA");

        [Fact]
        public void GenerateRefreshToken_UniqueEachCall()
        {
            OtpGenerator.GenerateRefreshToken()
                .Should().NotBe(OtpGenerator.GenerateRefreshToken());
        }
    }

    // ════════════════════════════════════════════════════════════
    // JWT TOKEN GENERATOR
    // ════════════════════════════════════════════════════════════
    public class JwtTokenGeneratorTests
    {
        private readonly JwtTokenGenerator _sut;

        public JwtTokenGeneratorTests()
        {
            // ✅ FIX: JwtSettings is in DigitalWallet.Application.Settings
            _sut = new JwtTokenGenerator(Options.Create(new JwtSettings
            {
                SecretKey = "",
                Issuer = "DigitalWallet.API",
                Audience = "DigitalWallet.Clients",
                ExpirationHours = 24
            }));
        }

        [Fact]
        public void GenerateToken_ReturnsThreePartJwt()
            => _sut.GenerateToken(TestDataBuilder.CreateUser()).Split('.').Should().HaveCount(3);

        [Fact]
        public void GenerateToken_ContainsUserEmail()
        {
            var user = TestDataBuilder.CreateUser();
            var decoded = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateToken(user));
            decoded.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        }

        [Fact]
        public void GenerateToken_ContainsUserId()
        {
            var user = TestDataBuilder.CreateUser();
            var decoded = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateToken(user));
            decoded.Claims.Should().Contain(c =>
                (c.Type == "nameid" || c.Type.Contains("nameidentifier")) &&
                c.Value == user.Id.ToString());
        }

        [Fact]
        public void GenerateToken_ExpiresIn24Hours()
        {
            var decoded = new JwtSecurityTokenHandler()
                .ReadJwtToken(_sut.GenerateToken(TestDataBuilder.CreateUser()));
            decoded.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_DifferentUsers_DifferentTokens()
        {
            var t1 = _sut.GenerateToken(TestDataBuilder.CreateUser());
            var t2 = _sut.GenerateToken(TestDataBuilder.CreateUser());
            t1.Should().NotBe(t2);
        }

        [Fact]
        public void GenerateToken_ContainsKycLevel()
        {
            var user = TestDataBuilder.CreateUser(u => u.KycLevel = KycLevel.Verified);
            var decoded = new JwtSecurityTokenHandler().ReadJwtToken(_sut.GenerateToken(user));
            decoded.Claims.Should().Contain(c => c.Type == "kycLevel" && c.Value == "Verified");
        }
    }

    // ════════════════════════════════════════════════════════════
    // CACHE KEYS
    // ════════════════════════════════════════════════════════════
    public class CacheKeysTests
    {
        [Fact]
        public void UserProfile_SameId_SameKey()
        {
            var id = Guid.NewGuid();
            CacheKeys.UserProfile(id).Should().Be(CacheKeys.UserProfile(id));
        }

        [Fact]
        public void WalletBalance_DifferentIds_DifferentKeys()
            => CacheKeys.WalletBalance(Guid.NewGuid())
                .Should().NotBe(CacheKeys.WalletBalance(Guid.NewGuid()));

        [Fact]
        public void ExchangeRate_ContainsBothCurrencies()
        {
            var key = CacheKeys.ExchangeRate("USD", "EGP");
            key.Should().Contain("USD").And.Contain("EGP");
        }

        [Fact]
        public void WalletTransactions_DifferentPages_DifferentKeys()
        {
            var id = Guid.NewGuid();
            CacheKeys.WalletTransactions(id, 1, 20)
                .Should().NotBe(CacheKeys.WalletTransactions(id, 2, 20));
        }

        [Fact]
        public void AllMethods_ReturnNonEmptyStrings()
        {
            var uid = Guid.NewGuid();
            var wid = Guid.NewGuid();
            CacheKeys.UserProfile(uid).Should().NotBeNullOrEmpty();
            CacheKeys.UserByEmail("t@t.com").Should().NotBeNullOrEmpty();
            CacheKeys.UserByPhone("010123456789").Should().NotBeNullOrEmpty();
            CacheKeys.UserWallets(uid).Should().NotBeNullOrEmpty();
            CacheKeys.WalletBalance(wid).Should().NotBeNullOrEmpty();
            CacheKeys.Wallet(wid).Should().NotBeNullOrEmpty();
            CacheKeys.ActiveBillers().Should().NotBeNullOrEmpty();
            CacheKeys.ExchangeRate("USD", "EGP").Should().NotBeNullOrEmpty();
            CacheKeys.UnreadNotificationCount(uid).Should().NotBeNullOrEmpty();
        }
    }
}