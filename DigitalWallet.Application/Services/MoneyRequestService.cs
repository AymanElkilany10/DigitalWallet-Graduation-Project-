using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.MoneyRequest;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Services
{
    public class MoneyRequestService : IMoneyRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MoneyRequestService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<MoneyRequestDto>> CreateRequestAsync(
            Guid fromUserId,
            CreateMoneyRequestDto request)
        {
            try
            {
                // Find receiver
                User? toUser = null;
                if (request.ToUserPhoneOrEmail.Contains("@"))
                    toUser = await _unitOfWork.Users.GetByEmailAsync(request.ToUserPhoneOrEmail);
                else
                    toUser = await _unitOfWork.Users.GetByPhoneNumberAsync(request.ToUserPhoneOrEmail);

                if (toUser == null)
                    return ServiceResult<MoneyRequestDto>.Failure("User not found");

                if (fromUserId == toUser.Id)
                    return ServiceResult<MoneyRequestDto>.Failure(
                        "Cannot request money from yourself");

                var moneyRequest = new MoneyRequest
                {
                    FromUserId = fromUserId,
                    ToUserId = toUser.Id,
                    Amount = request.Amount,
                    CurrencyCode = request.CurrencyCode,
                    Status = MoneyRequestStatus.Pending
                };

                await _unitOfWork.MoneyRequests.AddAsync(moneyRequest);

                // Create notification for receiver
                var notification = new Notification
                {
                    UserId = toUser.Id,
                    Title = "Money Request",
                    Body = $"Money request for {request.Amount} {request.CurrencyCode}",
                    Type = NotificationType.Transaction,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                var requestDto = _mapper.Map<MoneyRequestDto>(moneyRequest);
                return ServiceResult<MoneyRequestDto>.Success(
                    requestDto,
                    "Money request sent successfully");
            }
            catch (Exception ex)
            {
                return ServiceResult<MoneyRequestDto>.Failure(
                    $"Error creating money request: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<MoneyRequestDto>>> GetSentRequestsAsync(Guid userId)
        {
            try
            {
                var requests = await _unitOfWork.MoneyRequests.GetByFromUserIdAsync(userId);
                var requestDtos = _mapper.Map<IEnumerable<MoneyRequestDto>>(requests);
                return ServiceResult<IEnumerable<MoneyRequestDto>>.Success(requestDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<MoneyRequestDto>>.Failure(
                    $"Error retrieving sent requests: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<MoneyRequestDto>>> GetReceivedRequestsAsync(
            Guid userId)
        {
            try
            {
                var requests = await _unitOfWork.MoneyRequests.GetByToUserIdAsync(userId);
                var requestDtos = _mapper.Map<IEnumerable<MoneyRequestDto>>(requests);
                return ServiceResult<IEnumerable<MoneyRequestDto>>.Success(requestDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<MoneyRequestDto>>.Failure(
                    $"Error retrieving received requests: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> AcceptOrRejectRequestAsync(
            Guid userId,
            AcceptRejectRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var moneyRequest = await _unitOfWork.MoneyRequests.GetByIdAsync(request.RequestId);
                if (moneyRequest == null)
                    return ServiceResult<bool>.Failure("Money request not found");

                if (moneyRequest.ToUserId != userId)
                    return ServiceResult<bool>.Failure("Unauthorized");

                if (moneyRequest.Status != MoneyRequestStatus.Pending)
                    return ServiceResult<bool>.Failure("Request already processed");

                if (!request.Accept)
                {
                    moneyRequest.Status = MoneyRequestStatus.Rejected;
                    await _unitOfWork.MoneyRequests.UpdateAsync(moneyRequest);
                    await _unitOfWork.CommitTransactionAsync();

                    return ServiceResult<bool>.Success(true, "Request rejected");
                }

                // Accept request - process transfer
                // Verify OTP
                if (string.IsNullOrEmpty(request.OtpCode))
                    return ServiceResult<bool>.Failure("OTP required");

                var otp = await _unitOfWork.OtpCodes.GetValidOtpAsync(
                    userId,
                    request.OtpCode,
                    OtpType.Transfer);

                if (otp == null)
                    return ServiceResult<bool>.Failure("Invalid or expired OTP");

                // Get wallets
                var senderWallet = await _unitOfWork.Wallets.GetByUserIdAndCurrencyAsync(
                    userId,
                    moneyRequest.CurrencyCode);
                var receiverWallet = await _unitOfWork.Wallets.GetByUserIdAndCurrencyAsync(
                    moneyRequest.FromUserId,
                    moneyRequest.CurrencyCode);

                if (senderWallet == null || receiverWallet == null)
                    return ServiceResult<bool>.Failure("Wallet not found");

                if (senderWallet.Balance < moneyRequest.Amount)
                    return ServiceResult<bool>.Failure("Insufficient balance");

                // Update balances
                senderWallet.Balance -= moneyRequest.Amount;
                receiverWallet.Balance += moneyRequest.Amount;

                await _unitOfWork.Wallets.UpdateAsync(senderWallet);
                await _unitOfWork.Wallets.UpdateAsync(receiverWallet);

                // Update request status
                moneyRequest.Status = MoneyRequestStatus.Accepted;
                await _unitOfWork.MoneyRequests.UpdateAsync(moneyRequest);

                // Mark OTP as used
                await _unitOfWork.OtpCodes.MarkAsUsedAsync(otp.Id);

                await _unitOfWork.CommitTransactionAsync();

                return ServiceResult<bool>.Success(true, "Request accepted and payment processed");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<bool>.Failure($"Error processing request: {ex.Message}");
            }
        }
    }
}