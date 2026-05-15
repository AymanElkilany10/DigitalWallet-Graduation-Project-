using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace DigitalWallet.Application.Services
{
    public interface IExternalExchangeRateService
    {
        Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency);
        Task<Dictionary<string, decimal>?> GetAllRatesAsync(string baseCurrency);
    }

    public class ExternalExchangeRateService : IExternalExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalExchangeRateService> _logger;

        private const string API_BASE_URL = "https://api.exchangerate-api.com/v4/latest/";

        public ExternalExchangeRateService(
            HttpClient httpClient,
            ILogger<ExternalExchangeRateService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // 🔹 Get single rate
        public async Task<decimal?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                var data = await GetApiResponseAsync(fromCurrency);

                if (data?.Rates != null &&
                    data.Rates.TryGetValue(toCurrency, out var rate))
                {
                    return (decimal)rate;
                }

                _logger.LogWarning("Rate not found for {From} -> {To}", fromCurrency, toCurrency);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate {From}->{To}", fromCurrency, toCurrency);
                return null;
            }
        }

        // 🔹 Get all rates
        public async Task<Dictionary<string, decimal>?> GetAllRatesAsync(string baseCurrency)
        {
            try
            {
                var data = await GetApiResponseAsync(baseCurrency);

                if (data?.Rates == null)
                    return null;

                return data.Rates.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (decimal)kvp.Value
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all rates for {Base}", baseCurrency);
                return null;
            }
        }

        // 🔹 Shared API call (DRY + reusable)
        private async Task<ExchangeRateApiResponse?> GetApiResponseAsync(string baseCurrency)
        {
            var url = $"{API_BASE_URL}{baseCurrency}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Exchange API failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExchangeRateApiResponse>();
        }

        private class ExchangeRateApiResponse
        {
            public string? Base { get; set; }
            public Dictionary<string, double>? Rates { get; set; }
        }
    }
}
