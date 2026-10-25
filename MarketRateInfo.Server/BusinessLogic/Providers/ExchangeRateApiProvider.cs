using MarketRateInfo.Server.BusinessLogic.Extensions;
using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using System.Text.Json.Serialization;

namespace MarketRateInfo.Server.BusinessLogic.ExternalRates.Providers;

/// <summary>
/// Provides market exchange rates using the ExchangeRate-API service.
/// </summary>
/// <param name="httpClient">The HTTP client used to send requests to the ExchangeRate-API.</param>
/// <param name="logger">The logger used for logging information and warnings.</param>
public sealed class ExchangeRateApiProvider(HttpClient httpClient, ILogger<ExchangeRateApiProvider> logger) : IMarketRateProvider
{
	private sealed record ExchangeRateResponse([property: JsonPropertyName("result")] string Result, [property: JsonPropertyName("conversion_rates")] Dictionary<string, decimal>? ConversionRates, [property: JsonPropertyName("rates")] Dictionary<string, decimal>? Rates);

	/// <summary>
	/// Determines whether both specified currencies are supported fiat currencies.
	/// </summary>
	/// <param name="fromCurrency">The source currency code.</param>
	/// <param name="toCurrency">The target currency code.</param>
	/// <returns>true if both currencies are supported fiat currencies; otherwise, false.</returns>
	public bool Supports(string fromCurrency, string toCurrency)
	{
		return RateProviderHelpers.IsFiatCurrency(fromCurrency) && RateProviderHelpers.IsFiatCurrency(toCurrency);
	}

	/// <summary>
	/// Retrieves historical exchange rates for the specified currency pair and dates from the external ExchangeRate-API.
	/// </summary>
	/// <param name="fromCurrency">The ISO currency code to convert from.</param>
	/// <param name="toCurrency">The ISO currency code to convert to.</param>
	/// <param name="dates">The collection of dates for which to retrieve exchange rates.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A read-only collection of exchange rates for the specified dates.</returns>
	/// <exception cref="HttpRequestException">Thrown when no rates are returned for the requested currency pair and dates.</exception>
	public async Task<IReadOnlyCollection<ExternalMarketRate>> GetRatesAsync(string fromCurrency, string toCurrency, IReadOnlyCollection<DateTime> dates, CancellationToken cancellationToken = default(CancellationToken))
	{
		fromCurrency = RateProviderHelpers.NormalizeCurrency(fromCurrency);
		toCurrency = RateProviderHelpers.NormalizeCurrency(toCurrency);
		List<DateTime> requestedDates = dates.Select((DateTime x) => x.Date).Distinct().Order()
			.ToList();
		if (requestedDates.Count == 0)
		{
			return Array.Empty<ExternalMarketRate>();
		}
		if (fromCurrency == toCurrency)
		{
			return requestedDates.Select((DateTime date) => new ExternalMarketRate(fromCurrency, toCurrency, 1m, date)).ToList();
		}
		List<ExternalMarketRate> marketRates = new List<ExternalMarketRate>();
		foreach (DateTime currentDate in requestedDates)
		{
			cancellationToken.ThrowIfCancellationRequested();
			string uri = $"v6/history/{Uri.EscapeDataString(fromCurrency)}/{currentDate.Year}/{currentDate.Month}/{currentDate.Day}";
			logger.LogInformation("Calling ExchangeRate-API History: {Uri}", uri);
			using HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				logger.LogWarning("ExchangeRate-API history request failed for date {Date} with status {Status}", currentDate.ToString("yyyy-MM-dd"), response.StatusCode);
				continue;
			}
			ExchangeRateResponse data = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>(cancellationToken);
			if ((object)data != null && string.Equals(data.Result, "success", StringComparison.OrdinalIgnoreCase))
			{
				Dictionary<string, decimal> ratesMap = data.ConversionRates ?? data.Rates;
				if (ratesMap != null && ratesMap.TryGetValue(toCurrency, out var rate) && rate > 0m)
				{
					marketRates.Add(new ExternalMarketRate(fromCurrency, toCurrency, rate, currentDate));
				}
			}
		}
		if (marketRates.Count == 0)
		{
			throw new HttpRequestException($"ExchangeRate-API did not return any rates for {fromCurrency}/{toCurrency} in requested dates.");
		}
		logger.LogInformation("ExchangeRate-API returned {Count} historical rates for {FromCurrency}/{ToCurrency}.", marketRates.Count, fromCurrency, toCurrency);
		return marketRates;
	}
}
