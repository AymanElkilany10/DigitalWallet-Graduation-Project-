using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.BillPayment;
using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Services
{
    public class BillPaymentService : IBillPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICachingService _cache;
        private readonly INotificationService _notificationService;

        public BillPaymentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService,
            ICachingService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all active billers (CACHED for 24 hours)
        /// </summary>
        public async Task<ServiceResult<IEnumerable<BillerDto>>> GetAllBillersAsync()
        {
            try
            {
                var cacheKey = CacheKeys.ActiveBillers();

                var billerDtos = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var billers = await _unitOfWork.Billers.GetActiveBillersAsync();
                        return _mapper.Map<IEnumerable<BillerDto>>(billers);
                    },
                    TimeSpan.FromHours(24) 
                );

                return ServiceResult<IEnumerable<BillerDto>>.Success(billerDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<BillerDto>>.Failure(
                    $"Error retrieving billers: {ex.Message}");
            }
        }

        /// <summary>
        /// Pay a bill with OTP verification
        /// </summary>
        public async Task<ServiceResult<BillPaymentDto>> PayBillAsync(
            Guid userId,
            PayBillRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var otp = await _unitOfWork.OtpCodes.GetValidOtpAsync(
                    userId,
                    request.OtpCode,
                    OtpType.Transfer);

                if (otp == null)
                    return ServiceResult<BillPaymentDto>.Failure("Invalid or expired OTP");

                var wallet = await _unitOfWork.Wallets.GetByIdAsync(request.WalletId);
                if (wallet == null || wallet.UserId != userId)
                    return ServiceResult<BillPaymentDto>.Failure("Wallet not found");

                if (wallet.Balance < request.Amount)
                    return ServiceResult<BillPaymentDto>.Failure("Insufficient balance");

                var biller = await _unitOfWork.Billers.GetByIdAsync(request.BillerId);
                if (biller == null || !biller.IsActive)
                    return ServiceResult<BillPaymentDto>.Failure("Biller not available");

                var billPayment = new BillPayment
                {
                    UserId = userId,
                    WalletId = wallet.Id,
                    BillerId = biller.Id,
                    Amount = request.Amount,
                    CurrencyCode = wallet.CurrencyCode,
                    Status = TransactionStatus.Success,
                    ReceiptPath = $"/receipts/bill_{Guid.NewGuid()}.pdf"
                };

                await _unitOfWork.BillPayments.AddAsync(billPayment);

                wallet.Balance -= request.Amount;
                await _unitOfWork.Wallets.UpdateAsync(wallet);

                var transaction = new Domain.Entities.Transaction
                {
                    WalletId = wallet.Id,
                    Type = TransactionType.Bill,
                    Amount = -request.Amount,
                    CurrencyCode = wallet.CurrencyCode,
                    Status = TransactionStatus.Success,
                    Description = $"Bill payment - {biller.Name}",
                    Reference = billPayment.Id.ToString()
                };

                await _unitOfWork.Transactions.AddAsync(transaction);

                await _unitOfWork.OtpCodes.MarkAsUsedAsync(otp.Id);

                var notification = new Notification
                {
                    UserId = userId,
                    Title = "Bill Payment Successful",
                    Body = $"Paid {request.Amount} {wallet.CurrencyCode} to {biller.Name}",
                    Type = NotificationType.Transaction,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.CommitTransactionAsync();
                await InvalidateCachesAsync(userId, wallet.Id);

                var paymentDto = _mapper.Map<BillPaymentDto>(billPayment);
                return ServiceResult<BillPaymentDto>.Success(
                    paymentDto,
                    "Bill payment successful");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<BillPaymentDto>.Failure(
                    $"Bill payment failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get payment history with pagination (CACHED for 1 minute)
        /// </summary>
        public async Task<ServiceResult<PaginatedResult<BillPaymentDto>>> GetPaymentHistoryAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var cacheKey = CacheKeys.UserBillPayments(userId, pageNumber, pageSize);

                var paginatedResult = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var payments = await _unitOfWork.BillPayments.GetByUserIdAsync(
                            userId, pageNumber, pageSize);
                        var totalCount = payments.Count();

                        var paymentDtos = _mapper.Map<List<BillPaymentDto>>(payments);
                        return PaginatedResult<BillPaymentDto>.Create(
                            paymentDtos, totalCount, pageNumber, pageSize);
                    },
                    TimeSpan.FromMinutes(1) 
                );

                return ServiceResult<PaginatedResult<BillPaymentDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return ServiceResult<PaginatedResult<BillPaymentDto>>.Failure(
                    $"Error retrieving payment history: {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 Invalidate all relevant caches after bill payment
        /// </summary>
        private async Task InvalidateCachesAsync(Guid userId, Guid walletId)
        {
            await _cache.RemoveAsync(CacheKeys.WalletBalance(walletId));

            await _cache.RemoveAsync(CacheKeys.Wallet(walletId));

            await _cache.RemoveAsync(CacheKeys.UserWallets(userId));

            await _cache.RemoveByPatternAsync(CacheKeys.TransactionPattern(walletId));

            await _cache.RemoveByPatternAsync($"bill-payments:user:{userId}*");

            await _cache.RemoveAsync(CacheKeys.UnreadNotificationCount(userId));
        }
    }
}