using AkkaNetDemo.Interfaces;
using AkkaNetDemo.Models;
using System.Text.Json;

namespace AkkaNetDemo.Services;

public sealed class BitcoinRateService : IBitcoinRateService
{
    private const string ApiDateFormat = "dd-MM-yyyy";
    private const string CoinGeckoUrlFormat = "https://api.coingecko.com/api/v3/coins/bitcoin/history?date={0}";

    private readonly HttpClient _httpClient;

    public BitcoinRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BtcRate> GetRateAsync(
        string date,
        CancellationToken cancellationToken = default)
    {
        var formattedDate = DateTime.Parse(date).ToString(ApiDateFormat);
        var url = string.Format(CoinGeckoUrlFormat, formattedDate);

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(content);

        if (!doc.RootElement.TryGetProperty("market_data", out var marketData))
            return new BtcRate(date, Constants.FailedPriceSentinel);

        var price = marketData
            .GetProperty("current_price")
            .GetProperty("usd")
            .GetDecimal();

        return new BtcRate(date, price);
    }
}