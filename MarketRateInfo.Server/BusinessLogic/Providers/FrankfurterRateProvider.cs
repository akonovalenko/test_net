using MarketRateInfo.Server.BusinessLogic.Extensions;
using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Interfaces;

namespace MarketRateInfo.Server.BusinessLogic.ExternalRates.Providers;

/// <summary>
/// Provides market exchange rates using the Frankfurter API for fiat currencies.
/// </summary>
/// <param name="httpClient">The HTTP client used to send requests to the Frankfurter API.</param>
/// <param name="logger">The logger used for logging information and errors.</param>
public sealed class FrankfurterRateProvider(HttpClient httpClient, ILogger<FrankfurterRateProvider> logger) : IMarketRateProvider
{
	/// <summary>
	/// Determines whether both specified currencies are supported fiat currencies.
	/// </summary>
	/// <param name="fromCurrency">The ISO code of the source currency.</param>
	/// <param name="toCurrency">The ISO code of the target currency.</param>
	/// <returns>true if both currencies are supported fiat currencies; otherwise, false.</returns>
	public bool Supports(string fromCurrency, string toCurrency)
	{
		return RateProviderHelpers.IsFiatCurrency(fromCurrency) && RateProviderHelpers.IsFiatCurrency(toCurrency);
	}

	/// <summary>
	/// Retrieves exchange rates for the specified currency pair and dates from the Frankfurter API.
	/// </summary>
	/// <param name="fromCurrency">The ISO currency code to convert from.</param>
	/// <param name="toCurrency">The ISO currency code to convert to.</param>
	/// <param name="dates">The collection of dates for which to retrieve exchange rates.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A read-only collection of exchange rates for the requested dates.</returns>
	/// <exception cref="HttpRequestException">Thrown when the Frankfurter API request fails or returns an empty response.</exception>
	public async Task<IReadOnlyCollection<ExternalMarketRate>> GetRatesAsync(string fromCurrency, string toCurrency, IReadOnlyCollection<DateTime> dates, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (dates.Count == 0)
		{
			return Array.Empty<ExternalMarketRate>();
		}
		fromCurrency = RateProviderHelpers.NormalizeCurrency(fromCurrency);
		toCurrency = RateProviderHelpers.NormalizeCurrency(toCurrency);
		List<DateTime> requestedDates = dates.Select((DateTime x) => x.Date).Distinct().Order()
			.ToList();
		if (fromCurrency == toCurrency)
		{
			return requestedDates.Select((DateTime date) => new ExternalMarketRate(fromCurrency, toCurrency, 1m, date)).ToList();
		}
		DateTime startDate = requestedDates[0];
		DateTime endDate = requestedDates[requestedDates.Count - 1];
		HashSet<DateTime> requestedDateSet = requestedDates.ToHashSet();
		string startStr = startDate.ToString("yyyy-MM-dd");
		string endStr = endDate.ToString("yyyy-MM-dd");
		string uri = $"v2/rates?base={Uri.EscapeDataString(fromCurrency)}&quotes={Uri.EscapeDataString(toCurrency)}&from={startStr}&to={endStr}";
		logger.LogInformation("Calling Frankfurter v2 API for {FromCurrency}/{ToCurrency} from {StartDate} to {EndDate}. Requested dates: {Count}", fromCurrency, toCurrency, startStr, endStr, requestedDates.Count);
		using HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			string body = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new HttpRequestException($"Frankfurter API request failed with status code {(int)response.StatusCode} ({response.StatusCode}). Response: {body}");
		}
		List<FrankfurterRateItem> items = await response.Content.ReadFromJsonAsync<List<FrankfurterRateItem>>(cancellationToken);
		if (items == null || items.Count == 0)
		{
			throw new HttpRequestException("Frankfurter API returned an empty response.");
		}
		List<ExternalMarketRate> marketRates = new List<ExternalMarketRate>();
		foreach (FrankfurterRateItem item in items)
		{
			if (!(item.Rate <= 0m) && !string.IsNullOrEmpty(item.Date))
			{
				DateTime rateDateUtc = RateProviderHelpers.ParseRateDateUtc(item.Date);
				if (requestedDateSet.Contains(rateDateUtc.Date))
				{
					marketRates.Add(new ExternalMarketRate(fromCurrency, toCurrency, item.Rate, rateDateUtc));
				}
			}
		}
		logger.LogInformation("Frankfurter returned {Count} requested rates for {FromCurrency}/{ToCurrency}.", marketRates.Count, fromCurrency, toCurrency);
		return marketRates;
	}
}
