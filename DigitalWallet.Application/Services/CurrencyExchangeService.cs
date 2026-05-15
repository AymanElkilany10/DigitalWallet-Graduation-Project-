using AutoMapper;
using DigitalWallet.Application.Common;
using DigitalWallet.Application.DTOs.Exchange;
using DigitalWallet.Application.Helpers;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Application.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IExternalExchangeRateService _externalRateService;
        private readonly ICachingService _cache;

        public CurrencyExchangeService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IExternalExchangeRateService externalRateService,
            ICachingService cache)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _externalRateService = externalRateService;
            _cache = cache;
        }

        public async Task<ServiceResult<ExchangeResponseDto>> ExchangeCurrencyAsync(ExchangeRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var fromWallet = await _unitOfWork.Wallets.GetByIdAsync(request.FromWalletId);
                var toWallet = await _unitOfWork.Wallets.GetByIdAsync(request.ToWalletId);

                if (fromWallet == null || toWallet == null)
                    return ServiceResult<ExchangeResponseDto>.Failure("Wallet not found");

                if (fromWallet.UserId != toWallet.UserId)
                    return ServiceResult<ExchangeResponseDto>.Failure("Wallets must belong to same user");

                if (fromWallet.CurrencyCode == toWallet.CurrencyCode)
                    return ServiceResult<ExchangeResponseDto>.Failure("Cannot exchange same currency");

                if (fromWallet.Balance < request.Amount)
                    return ServiceResult<ExchangeResponseDto>.Failure("Insufficient balance");

                var rate = await GetCurrentExchangeRateAsync(fromWallet.CurrencyCode,toWallet.CurrencyCode);

                if (rate == null)
                    return ServiceResult<ExchangeResponseDto>.Failure("Exchange rate unavailable");

                var fee = CalculateExchangeFee(request.Amount);
                var totalDeducted = request.Amount + fee;
                var convertedAmount = request.Amount * rate.Value;

                if (fromWallet.Balance < totalDeducted)
                    return ServiceResult<ExchangeResponseDto>.Failure("Insufficient balance including fee");

                fromWallet.Balance -= totalDeducted;
                toWallet.Balance += convertedAmount;

                await _unitOfWork.Wallets.UpdateAsync(fromWallet);
                await _unitOfWork.Wallets.UpdateAsync(toWallet);

                var exchange = new CurrencyExchange
                {
                    UserId = fromWallet.UserId,
                    FromWalletId = fromWallet.Id,
                    ToWalletId = toWallet.Id,
                    FromAmount = request.Amount,
                    FromCurrency = fromWallet.CurrencyCode,
                    ToAmount = convertedAmount,
                    ToCurrency = toWallet.CurrencyCode,
                    ExchangeRate = rate.Value,
                    Fee = fee,
                    Status = "Success"
                };

                await _unitOfWork.CurrencyExchanges.AddAsync(exchange);

                await _unitOfWork.Transactions.AddAsync(new Transaction
                {
                    WalletId = fromWallet.Id,
                    Type = TransactionType.Withdraw,
                    Amount = -totalDeducted,
                    CurrencyCode = fromWallet.CurrencyCode,
                    Status = TransactionStatus.Success,
                    Description = $"Exchange to {toWallet.CurrencyCode}",
                    Reference = exchange.Id.ToString()
                });

                await _unitOfWork.Transactions.AddAsync(new Transaction
                {
                    WalletId = toWallet.Id,
                    Type = TransactionType.Deposit,
                    Amount = convertedAmount,
                    CurrencyCode = toWallet.CurrencyCode,
                    Status = TransactionStatus.Success,
                    Description = $"Exchange from {fromWallet.CurrencyCode}",
                    Reference = exchange.Id.ToString()
                });

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    UserId = fromWallet.UserId,
                    Title = "Currency Exchange Successful",
                    Body = $"Exchanged {request.Amount} {fromWallet.CurrencyCode} to {convertedAmount:F2} {toWallet.CurrencyCode}",
                    Type = NotificationType.Transaction,
                    IsRead = false
                });

                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCachesAsync(fromWallet.UserId, fromWallet.Id, toWallet.Id);

                var response = new ExchangeResponseDto
                {
                    ExchangeId = exchange.Id,
                    FromAmount = request.Amount,
                    FromCurrency = fromWallet.CurrencyCode,
                    ToAmount = convertedAmount,
                    ToCurrency = toWallet.CurrencyCode,
                    ExchangeRate = rate.Value,
                    Fee = fee,
                    Status = "Success",
                    ExchangedAt = DateTime.UtcNow
                };

                return ServiceResult<ExchangeResponseDto>.Success(response, "Exchange successful");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResult<ExchangeResponseDto>.Failure($"Exchange failed: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ExchangeRateDto>> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            var rate = await GetCurrentExchangeRateAsync(fromCurrency, toCurrency);

            if (rate == null)
                return ServiceResult<ExchangeRateDto>.Failure("Rate not available");

            return ServiceResult<ExchangeRateDto>.Success(new ExchangeRateDto
            {
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Rate = rate.Value,
                LastUpdated = DateTime.UtcNow
            });
        }

        public async Task<ServiceResult<List<ExchangeRateDto>>> GetAllExchangeRatesAsync(string baseCurrency)
        {
            try
            {
                var cacheKey = CacheKeys.AllExchangeRates();

                var rates = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        var dict = await _externalRateService.GetAllRatesAsync(baseCurrency)
                                   ?? new Dictionary<string, decimal>();

                        return dict.Select(x => new ExchangeRateDto
                        {
                            FromCurrency = baseCurrency,
                            ToCurrency = x.Key,
                            Rate = x.Value,
                            LastUpdated = DateTime.UtcNow
                        }).ToList();
                    },
                    TimeSpan.FromHours(1)
                );

                return ServiceResult<List<ExchangeRateDto>>.Success(rates);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ExchangeRateDto>>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<List<ExchangeResponseDto>>> GetUserExchangeHistoryAsync(
            Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            var exchanges = await _unitOfWork.CurrencyExchanges
                .GetUserExchangesAsync(userId, pageNumber, pageSize);

            var dtos = _mapper.Map<List<ExchangeResponseDto>>(exchanges);
            return ServiceResult<List<ExchangeResponseDto>>.Success(dtos);
        }

        public async Task<ServiceResult<bool>> UpdateExchangeRatesAsync()
        {
            var currencies = new[] { "USD", "EUR", "GBP", "EGP" };

            foreach (var from in currencies)
            {
                var rates = await _externalRateService.GetAllRatesAsync(from);
                if (rates == null) continue;

                foreach (var to in rates)
                {
                    var existing = await _unitOfWork.ExchangeRates.GetRateAsync(from, to.Key);

                    if (existing != null)
                    {
                        existing.Rate = to.Value;
                        existing.LastUpdated = DateTime.UtcNow;
                        await _unitOfWork.ExchangeRates.UpdateAsync(existing);
                    }
                    else
                    {
                        await _unitOfWork.ExchangeRates.AddAsync(new ExchangeRate
                        {
                            FromCurrency = from,
                            ToCurrency = to.Key,
                            Rate = to.Value,
                            IsActive = true
                        });
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            await _cache.RemoveAsync(CacheKeys.AllExchangeRates());

            return ServiceResult<bool>.Success(true, "Rates updated");
        }

        private async Task<decimal?> GetCurrentExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            var cacheKey = CacheKeys.ExchangeRate(fromCurrency, toCurrency);

            return await _cache.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var dbRate = await _unitOfWork.ExchangeRates.GetRateAsync(fromCurrency, toCurrency);
                    if (dbRate != null)
                        return (decimal?)dbRate.Rate;

                    return await _externalRateService.GetExchangeRateAsync(fromCurrency, toCurrency);
                },
                TimeSpan.FromHours(1)
            );
        }

        private decimal CalculateExchangeFee(decimal amount) => amount * 0.005m;

        private async Task InvalidateCachesAsync(Guid userId, Guid fromWalletId, Guid toWalletId)
        {
            await _cache.RemoveAsync(CacheKeys.UserWallets(userId));
            await _cache.RemoveAsync(CacheKeys.WalletBalance(fromWalletId));
            await _cache.RemoveAsync(CacheKeys.WalletBalance(toWalletId));
            await _cache.RemoveAsync(CacheKeys.Wallet(fromWalletId));
            await _cache.RemoveAsync(CacheKeys.Wallet(toWalletId));
            await _cache.RemoveByPatternAsync(CacheKeys.TransactionPattern(fromWalletId));
            await _cache.RemoveByPatternAsync(CacheKeys.TransactionPattern(toWalletId));
        }
    }
}
