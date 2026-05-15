using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.FakeBank;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Services
{
    public class FakeBankService : IFakeBankService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FakeBankService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<FakeBankTransactionDto>> DepositAsync(DepositRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var bankAccount = await _unitOfWork.FakeBankAccounts.GetByUserIdAsync(request.UserId);
                if (bankAccount == null)
                    return ServiceResult<FakeBankTransactionDto>.Failure("Bank account not found");

                if (bankAccount.Balance < request.Amount)
                    return ServiceResult<FakeBankTransactionDto>.Failure(
                        "Insufficient bank balance");

                var wallet = await _unitOfWork.Wallets.GetByUserIdAndCurrencyAsync(
                    request.UserId, "EGP");
                if (wallet == null)
                    return ServiceResult<FakeBankTransactionDto>.Failure("Wallet not found");

                // Create fake bank transaction
                var bankTransaction = new FakeBankTransaction
                {
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Type = "deposit",
                    Status = TransactionStatus.Success,
                    DelaySeconds = 5 // Simulate processing delay
                };

                await _unitOfWork.FakeBankTransactions.AddAsync(bankTransaction);

                // Simulate delay (in production, use background job)
                // For now, process immediately
                bankAccount.Balance -= request.Amount;
                wallet.Balance += request.Amount;

                await _unitOfWork.FakeBankAccounts.UpdateAsync(bankAccount);
                await _unitOfWork.Wallets.UpdateAsync(wallet);

                // Create transaction record
                var transaction = new Domain.Entities.Transaction
                {
                    WalletId = wallet.Id,
                    Type = TransactionType.Deposit,
                    Amount = request.Amount,
                    CurrencyCode = "EGP",
                    Status = TransactionStatus.Success,
                    Description = "Deposit from bank account",
                    Reference = bankTransaction.Id.ToString()
                };

                await _unitOfWork.Transactions.AddAsync(transaction);

                // Create notification
                var notification = new Notification
                {
                    UserId = request.UserId,
                    Title = "Deposit Successful",
                    Body = $"Deposited {request.Amount} EGP to your wallet",
                    Type = NotificationType.Transaction,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);

                await _unitOfWork.CommitTransactionAsync();

                var transactionDto = _mapper.Map<FakeBankTransactionDto>(bankTransaction);
                return ServiceResult<FakeBankTransactionDto>.Success(
                    transactionDto,
                    "Deposit successful");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<FakeBankTransactionDto>.Failure(
                    $"Deposit failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<FakeBankTransactionDto>> WithdrawAsync(WithdrawRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _unitOfWork.Wallets.GetByUserIdAndCurrencyAsync(
                    request.UserId, "EGP");
                if (wallet == null)
                    return ServiceResult<FakeBankTransactionDto>.Failure("Wallet not found");

                if (wallet.Balance < request.Amount)
                    return ServiceResult<FakeBankTransactionDto>.Failure(
                        "Insufficient wallet balance");

                var bankAccount = await _unitOfWork.FakeBankAccounts.GetByUserIdAsync(request.UserId);
                if (bankAccount == null)
                    return ServiceResult<FakeBankTransactionDto>.Failure("Bank account not found");

                // Create fake bank transaction
                var bankTransaction = new FakeBankTransaction
                {
                    UserId = request.UserId,
                    Amount = request.Amount,
                    Type = "withdraw",
                    Status = TransactionStatus.Success,
                    DelaySeconds = 5
                };

                await _unitOfWork.FakeBankTransactions.AddAsync(bankTransaction);

                // Process withdrawal
                wallet.Balance -= request.Amount;
                bankAccount.Balance += request.Amount;

                await _unitOfWork.Wallets.UpdateAsync(wallet);
                await _unitOfWork.FakeBankAccounts.UpdateAsync(bankAccount);

                // Create transaction record
                var transaction = new Domain.Entities.Transaction
                {
                    WalletId = wallet.Id,
                    Type = TransactionType.Withdraw,
                    Amount = -request.Amount,
                    CurrencyCode = "EGP",
                    Status = TransactionStatus.Success,
                    Description = "Withdrawal to bank account",
                    Reference = bankTransaction.Id.ToString()
                };

                await _unitOfWork.Transactions.AddAsync(transaction);

                // Create notification
                var notification = new Notification
                {
                    UserId = request.UserId,
                    Title = "Withdrawal Successful",
                    Body = $"Withdrawn {request.Amount} EGP from your wallet",
                    Type = NotificationType.Transaction,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);

                await _unitOfWork.CommitTransactionAsync();

                var transactionDto = _mapper.Map<FakeBankTransactionDto>(bankTransaction);
                return ServiceResult<FakeBankTransactionDto>.Success(
                    transactionDto,
                    "Withdrawal successful");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<FakeBankTransactionDto>.Failure(
                    $"Withdrawal failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<decimal>> GetBankBalanceAsync(Guid userId)
        {
            try
            {
                var bankAccount = await _unitOfWork.FakeBankAccounts.GetByUserIdAsync(userId);
                if (bankAccount == null)
                    return ServiceResult<decimal>.Failure("Bank account not found");

                return ServiceResult<decimal>.Success(bankAccount.Balance);
            }
            catch (Exception ex)
            {
                return ServiceResult<decimal>.Failure(
                    $"Error retrieving bank balance: {ex.Message}");
            }
        }
    }
}