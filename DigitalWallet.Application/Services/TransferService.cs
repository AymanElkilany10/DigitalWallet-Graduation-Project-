using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.Transfer;
using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Services
{
    public class TransferService : ITransferService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICachingService _cache;

        public TransferService(IUnitOfWork unitOfWork, IMapper mapper, ICachingService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ServiceResult<TransferResponseDto>> SendMoneyAsync(SendMoneyRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var senderWallet = await _unitOfWork.Wallets.GetByIdAsync(request.SenderWalletId);
                if (senderWallet == null)
                    return ServiceResult<TransferResponseDto>.Failure("Sender wallet not found");

                var otp = await _unitOfWork.OtpCodes.GetValidOtpAsync(
                    senderWallet.UserId,
                    request.OtpCode,
                    OtpType.Transfer);

                if (otp == null)
                    return ServiceResult<TransferResponseDto>.Failure("Invalid or expired OTP");

                User? receiver = request.ReceiverPhoneOrEmail.Contains("@")
                    ? await _unitOfWork.Users.GetByEmailAsync(request.ReceiverPhoneOrEmail)
                    : await _unitOfWork.Users.GetByPhoneNumberAsync(request.ReceiverPhoneOrEmail);

                if (receiver == null)
                    return ServiceResult<TransferResponseDto>.Failure("Receiver not found");

                var receiverWallet = await _unitOfWork.Wallets.GetByUserIdAndCurrencyAsync(
                    receiver.Id,
                    senderWallet.CurrencyCode);

                if (receiverWallet == null)
                    return ServiceResult<TransferResponseDto>.Failure(
                        $"Receiver doesn't have a {senderWallet.CurrencyCode} wallet");

                if (senderWallet.Balance < request.Amount)
                    return ServiceResult<TransferResponseDto>.Failure("Insufficient balance");

                // Create transfer
                var transfer = new Transfer
                {
                    SenderWalletId = senderWallet.Id,
                    ReceiverWalletId = receiverWallet.Id,
                    Amount = request.Amount,
                    CurrencyCode = senderWallet.CurrencyCode,
                    Status = TransactionStatus.Success
                };

                await _unitOfWork.Transfers.AddAsync(transfer);

                // Update balances
                senderWallet.Balance -= request.Amount;
                receiverWallet.Balance += request.Amount;

                await _unitOfWork.Wallets.UpdateAsync(senderWallet);
                await _unitOfWork.Wallets.UpdateAsync(receiverWallet);

                // Transactions
                await _unitOfWork.Transactions.AddAsync(new Transaction
                {
                    WalletId = senderWallet.Id,
                    Type = TransactionType.Transfer,
                    Amount = -request.Amount,
                    CurrencyCode = senderWallet.CurrencyCode,
                    Status = TransactionStatus.Success,
                    Description = request.Description ?? $"Transfer to {receiver.FullName}",
                    Reference = transfer.Id.ToString()
                });

                await _unitOfWork.Transactions.AddAsync(new Transaction
                {
                    WalletId = receiverWallet.Id,
                    Type = TransactionType.Transfer,
                    Amount = request.Amount,
                    CurrencyCode = receiverWallet.CurrencyCode,
                    Status = TransactionStatus.Success,
                    Description = $"Transfer from {senderWallet.User?.FullName ?? "User"}",
                    Reference = transfer.Id.ToString()
                });

                await _unitOfWork.OtpCodes.MarkAsUsedAsync(otp.Id);

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    UserId = senderWallet.UserId,
                    Title = "Transfer Sent",
                    Body = $"You sent {request.Amount} {senderWallet.CurrencyCode} to {receiver.FullName}",
                    Type = NotificationType.Transaction
                });

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    UserId = receiver.Id,
                    Title = "Money Received",
                    Body = $"You received {request.Amount} {receiverWallet.CurrencyCode}",
                    Type = NotificationType.Transaction
                });

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // ❗ IMPORTANT: Invalidate cache after write
                await _cache.RemoveByPatternAsync(CacheKeys.TransactionPattern(senderWallet.Id));
                await _cache.RemoveByPatternAsync(CacheKeys.TransactionPattern(receiverWallet.Id));
                await _cache.RemoveAsync(CacheKeys.WalletBalance(senderWallet.Id));
                await _cache.RemoveAsync(CacheKeys.WalletBalance(receiverWallet.Id));

                var response = new TransferResponseDto
                {
                    TransferId = transfer.Id,
                    ReceiverName = receiver.FullName,
                    Amount = request.Amount,
                    CurrencyCode = senderWallet.CurrencyCode,
                    Status = "Success",
                    TransferredAt = DateTime.UtcNow
                };

                return ServiceResult<TransferResponseDto>.Success(response, "Transfer completed successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<TransferResponseDto>.Failure($"Transfer failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PaginatedResult<TransferDto>>> GetTransferHistoryAsync(
            Guid walletId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var cacheKey = CacheKeys.WalletTransactions(walletId, pageNumber, pageSize);

                var result = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var sent = await _unitOfWork.Transfers.GetBySenderWalletIdAsync(walletId, pageNumber, pageSize);
                        var received = await _unitOfWork.Transfers.GetByReceiverWalletIdAsync(walletId, pageNumber, pageSize);

                        var all = sent.Concat(received)
                                      .OrderByDescending(t => t.CreatedAt)
                                      .Take(pageSize)
                                      .ToList();

                        var dto = _mapper.Map<List<TransferDto>>(all);

                        return PaginatedResult<TransferDto>.Create(dto, all.Count, pageNumber, pageSize);
                    },
                    TimeSpan.FromMinutes(10)
                );

                return ServiceResult<PaginatedResult<TransferDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PaginatedResult<TransferDto>>.Failure(
                    $"Error retrieving transfer history: {ex.Message}");
            }
        }
    }
}
