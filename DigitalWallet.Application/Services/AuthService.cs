using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.ExternalServices.SMS;
using DigitalWallet.Application.ExternalServices.Email;
using Microsoft.Extensions.Options;
using DigitalWallet.Application.Settings;

namespace DigitalWallet.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly ICachingService _cache;
        private readonly NotificationSettings _notificationSettings;

        public AuthService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            JwtTokenGenerator jwtTokenGenerator,
            ISmsService smsService,
            IEmailService emailService,
            ICachingService cache,
            IOptions<NotificationSettings> notificationSettings)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jwtTokenGenerator = jwtTokenGenerator;
            _smsService = smsService;
            _emailService = emailService;
            _cache = cache;
            _notificationSettings = notificationSettings.Value;
        }

        // ═══════════════════════════════════════════════════════════
        // REGISTER
        // ═══════════════════════════════════════════════════════════
        public async Task<ServiceResult<LoginResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                // Validate email/phone uniqueness
                if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
                    return ServiceResult<LoginResponseDto>.Failure("Email already registered");

                if (await _unitOfWork.Users.PhoneExistsAsync(request.PhoneNumber))
                    return ServiceResult<LoginResponseDto>.Failure("Phone number already registered");

                // Hash password
                var salt = PasswordHasher.GenerateSalt();
                var passwordHash = PasswordHasher.HashPassword(request.Password, salt);

                // Create user
                var user = _mapper.Map<User>(request);
                user.PasswordHash = passwordHash;
                user.Salt = salt;
                user.KycLevel = KycLevel.Basic;
                user.Status = UserStatus.Active;

                await _unitOfWork.Users.AddAsync(user);

                // Create default wallet
                var wallet = new Wallet
                {
                    UserId = user.Id,
                    CurrencyCode = "EGP",
                    Balance = 0,
                    DailyLimit = 5000,
                    MonthlyLimit = 20000
                };

                await _unitOfWork.Wallets.AddAsync(wallet);

                // Create fake bank account
                var fakeBankAccount = new FakeBankAccount
                {
                    UserId = user.Id,
                    AccountNumber = OtpGenerator.GenerateAccountNumber(),
                    Balance = 10000
                };

                await _unitOfWork.FakeBankAccounts.AddAsync(fakeBankAccount);
                await _unitOfWork.SaveChangesAsync();

                // 🆕 Send welcome email & SMS (fire and forget)
                if (_notificationSettings.SendWelcomeEmail)
                {
                    _ = Task.Run(async () =>
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
                    });
                }

                if (_notificationSettings.SendWelcomeSms)
                {
                    _ = Task.Run(async () =>
                    {
                        await _smsService.SendWelcomeSmsAsync(user.PhoneNumber, user.FullName);
                    });
                }

                // Generate token
                var token = _jwtTokenGenerator.GenerateToken(user);
                var refreshToken = OtpGenerator.GenerateRefreshToken();

                var response = new LoginResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    RequiresOtp = false
                };

                return ServiceResult<LoginResponseDto>.Success(response, "Registration successful! Welcome email sent.");
            }
            catch (Exception ex)
            {
                return ServiceResult<LoginResponseDto>.Failure($"Registration failed: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // LOGIN (Send OTP)
        // ═══════════════════════════════════════════════════════════
        public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            try
            {
                // 🆕 Check cache first for user by email/phone
                User? user;

                if (request.EmailOrPhone.Contains("@"))
                {
                    var cacheKey = CacheKeys.UserByEmail(request.EmailOrPhone);
                    user = await _cache.GetOrSetAsync(
                        cacheKey,
                        async () => await _unitOfWork.Users.GetByEmailAsync(request.EmailOrPhone),
                        TimeSpan.FromMinutes(10)
                    );
                }
                else
                {
                    var cacheKey = CacheKeys.UserByPhone(request.EmailOrPhone);
                    user = await _cache.GetOrSetAsync(
                        cacheKey,
                        async () => await _unitOfWork.Users.GetByPhoneNumberAsync(request.EmailOrPhone),
                        TimeSpan.FromMinutes(10)
                    );
                }

                if (user == null)
                    return ServiceResult<LoginResponseDto>.Failure("Invalid credentials");

                // Verify password
                if (!PasswordHasher.VerifyPassword(request.Password, user.Salt, user.PasswordHash))
                    return ServiceResult<LoginResponseDto>.Failure("Invalid credentials");

                if (user.Status != UserStatus.Active)
                    return ServiceResult<LoginResponseDto>.Failure("Account is suspended");

                // Generate OTP
                var otpCode = OtpGenerator.GenerateOtpCode();

                var otp = new OtpCode
                {
                    UserId = user.Id,
                    Code = otpCode,
                    Type = OtpType.Login,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };

                await _unitOfWork.OtpCodes.AddAsync(otp);
                await _unitOfWork.SaveChangesAsync();

                // 🆕 Send OTP via Email AND SMS (fire and forget)
                if (_notificationSettings.SendOtpViaEmail)
                {
                    _ = Task.Run(async () =>
                    {
                        await _emailService.SendOtpEmailAsync(user.Email, otpCode);
                    });
                }

                if (_notificationSettings.SendOtpViaSms)
                {
                    _ = Task.Run(async () =>
                    {
                        await _smsService.SendOtpAsync(user.PhoneNumber, otpCode);
                    });
                }

                var response = new LoginResponseDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    RequiresOtp = true,
                    Token = string.Empty,
                    RefreshToken = string.Empty
                };

#if DEBUG
                // In development, include OTP in response for easier testing
                return ServiceResult<LoginResponseDto>.Success(response, $"OTP sent to your email and phone. Code: {otpCode}");
#else
                return ServiceResult<LoginResponseDto>.Success(response, "OTP sent to your email and phone");
#endif
            }
            catch (Exception ex)
            {
                return ServiceResult<LoginResponseDto>.Failure($"Login failed: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // VERIFY OTP
        // ═══════════════════════════════════════════════════════════
        public async Task<ServiceResult<LoginResponseDto>> VerifyOtpAsync(VerifyOtpRequestDto request)
        {
            try
            {
                var otp = await _unitOfWork.OtpCodes.GetValidOtpAsync(
                    request.UserId,
                    request.Code,
                    OtpType.Login);

                if (otp == null)
                    return ServiceResult<LoginResponseDto>.Failure("Invalid or expired OTP");

                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
                if (user == null)
                    return ServiceResult<LoginResponseDto>.Failure("User not found");

                // Mark OTP as used
                await _unitOfWork.OtpCodes.MarkAsUsedAsync(otp.Id);

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // 🆕 Invalidate user cache
                await _cache.RemoveAsync(CacheKeys.UserProfile(user.Id));
                await _cache.RemoveAsync(CacheKeys.UserByEmail(user.Email));
                await _cache.RemoveAsync(CacheKeys.UserByPhone(user.PhoneNumber));

                // Generate tokens
                var token = _jwtTokenGenerator.GenerateToken(user);
                var refreshToken = OtpGenerator.GenerateRefreshToken();

                var response = new LoginResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    RequiresOtp = false
                };

                return ServiceResult<LoginResponseDto>.Success(response, "Login successful");
            }
            catch (Exception ex)
            {
                return ServiceResult<LoginResponseDto>.Failure($"OTP verification failed: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // SEND OTP (Manual Request)
        // ═══════════════════════════════════════════════════════════
        public async Task<ServiceResult<bool>> SendOtpAsync(Guid userId, string otpType)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<bool>.Failure("User not found");

                var otpCode = OtpGenerator.GenerateOtpCode();
                var type = Enum.Parse<OtpType>(otpType, true);

                var otp = new OtpCode
                {
                    UserId = userId,
                    Code = otpCode,
                    Type = type,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };

                await _unitOfWork.OtpCodes.AddAsync(otp);
                await _unitOfWork.SaveChangesAsync();

                // Send via Email AND SMS
                if (_notificationSettings.SendOtpViaEmail)
                {
                    _ = Task.Run(async () =>
                    {
                        await _emailService.SendOtpEmailAsync(user.Email, otpCode);
                    });
                }

                if (_notificationSettings.SendOtpViaSms)
                {
                    _ = Task.Run(async () =>
                    {
                        await _smsService.SendOtpAsync(user.PhoneNumber, otpCode);
                    });
                }

#if DEBUG
                return ServiceResult<bool>.Success(true, $"OTP sent. Code: {otpCode}");
#else
                return ServiceResult<bool>.Success(true, "OTP sent to your email and phone");
#endif
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Failed to send OTP: {ex.Message}");
            }
        }
    }
}