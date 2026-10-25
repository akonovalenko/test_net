using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models.Requests;
using MarketRateInfo.Server.DataAccess.Entities;
using MarketRateInfo.Server.DataAccess.Repositories;

namespace MarketRateInfo.Server.BusinessLogic.Services;

/// <summary>
/// Provides functionality to refresh and persist market rates using configured providers and repository.
/// </summary>
/// <param name="repository">Repository for accessing and persisting market rate data.</param>
/// <param name="providers">Collection of market rate providers used to fetch external rates.</param>
/// <param name="logger">Logger for diagnostic and operational messages.</param>
public sealed class MarketRateRefreshService(IMarketRateRepository repository, IEnumerable<IMarketRateProvider> providers, ILogger<MarketRateRefreshService> logger) : IMarketRateRefreshService
{
	/// <summary>
	/// Refreshes market rates for all configured currency pairs within the specified date range using available providers.
	/// </summary>
	/// <param name="startDate">The start date of the period for which to refresh market rates.</param>
	/// <param name="endDate">The end date of the period for which to refresh market rates.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A result containing details of the market rate refresh operation.</returns>
	/// <exception cref="InvalidOperationException">Thrown when no currency pairs or market rate providers are configured.</exception>
	public async Task<MarketRateRefreshResult> RefreshAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default(CancellationToken))
	{
		List<CurrencyPairRequest> pairs = (from x in (await repository.GetSymbolsAsync(cancellationToken)).Select(ParseSymbol)
			where (object)x != null
			select (x)).ToList();
		if (pairs.Count == 0)
		{
			throw new InvalidOperationException("No currency pairs are configured in the database.");
		}
		List<IMarketRateProvider> providerList = providers.ToList();
		if (providerList.Count == 0)
		{
			throw new InvalidOperationException("No market rate providers are configured.");
		}
		List<ExternalMarketRate> rates = new List<ExternalMarketRate>();
		foreach (CurrencyPairRequest pair in pairs)
		{
			List<ExternalMarketRate> list = rates;
			list.AddRange(await GetRateFromProvidersAsync(providerList, pair, startDate, endDate, cancellationToken));
		}
		return await SaveRatesAsync(rates, pairs.Count, providerList.Count, cancellationToken);
	}

	/// <summary>
	/// Refreshes market rates for the specified currency pair and date range using configured providers.
	/// </summary>
	/// <param name="symbol">Currency pair symbol in the format BASE/QUOTE (e.g. EUR/USD).</param>
	/// <param name="startDate">Start date of the rate retrieval period.</param>
	/// <param name="endDate">End date of the rate retrieval period.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the market rate refresh result.</returns>
	/// <exception cref="ArgumentException">Thrown when the symbol is not in the required format.</exception>
	/// <exception cref="InvalidOperationException">Thrown when no market rate providers are configured.</exception>
	public async Task<MarketRateRefreshResult> RefreshAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default(CancellationToken))
	{
		CurrencyPairRequest pair = ParseSymbol(symbol) ?? throw new ArgumentException("Symbol must be in format BASE/QUOTE (e.g. EUR/USD).", "symbol");
		List<IMarketRateProvider> providerList = providers.ToList();
		logger.LogInformation("Starting market-rate refresh for {Symbol}. Providers: {Providers}", symbol, string.Join(", ", providerList.Select((IMarketRateProvider x) => x.GetType().Name)));
		if (providerList.Count == 0)
		{
			throw new InvalidOperationException("No market rate providers are configured.");
		}
		return await SaveRatesAsync((await GetRateFromProvidersAsync(providerList, pair, startDate, endDate, cancellationToken)).ToList(), 1, providerList.Count, cancellationToken);
	}

	/// <summary>
	/// Fetches market rates for the specified currency pair and date range from the provided rate providers, skipping
	/// dates already present in the repository.
	/// </summary>
	/// <param name="providerList">The list of market rate providers to query.</param>
	/// <param name="pair">The currency pair request containing base and quote currencies.</param>
	/// <param name="startDate">The start date of the range to fetch rates for.</param>
	/// <param name="endDate">The end date of the range to fetch rates for.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A read-only collection of external market rates retrieved from the providers.</returns>
	/// <exception cref="InvalidOperationException">Thrown when no rates could be fetched for the specified currency pair from any provider.</exception>
	private async Task<IReadOnlyCollection<ExternalMarketRate>> GetRateFromProvidersAsync(List<IMarketRateProvider> providerList, CurrencyPairRequest pair, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
	{
		string symbol = pair.BaseCurrency + "/" + pair.QuoteCurrency;
		IReadOnlyList<DateTime> missingDates = await repository.GetMissingDatesAsync(symbol, startDate, endDate, cancellationToken);
		if (missingDates.Count == 0)
		{
			logger.LogInformation("Skipping {Pair}: all dates from {StartDate:d} to {EndDate:d} already have rates.", symbol, startDate, endDate);
			return Array.Empty<ExternalMarketRate>();
		}
		logger.LogInformation("Found {MissingCount} missing dates for {Pair} between {StartDate:d} and {EndDate:d}.", missingDates.Count, symbol, startDate, endDate);
		HashSet<DateTime> remainingDates = missingDates.Select((DateTime x) => x.Date).ToHashSet();
		List<ExternalMarketRate> result = new List<ExternalMarketRate>();
		foreach (IMarketRateProvider provider in providerList)
		{
			if (remainingDates.Count == 0)
			{
				break;
			}
			if (!provider.Supports(pair.BaseCurrency, pair.QuoteCurrency))
			{
				logger.LogDebug("Skipping provider {Provider} for {Pair}: currency pair is not supported.", provider.GetType().Name, symbol);
				continue;
			}
			DateTime[] datesForProvider = remainingDates.Order().ToArray();
			logger.LogInformation("Trying provider {Provider} for {Pair}. Missing dates: {Count}.", provider.GetType().Name, symbol, datesForProvider.Length);
			try
			{
				IReadOnlyCollection<ExternalMarketRate> rates = await provider.GetRatesAsync(pair.BaseCurrency, pair.QuoteCurrency, datesForProvider, cancellationToken);
				if (rates.Count == 0)
				{
					logger.LogWarning("Provider {Provider} returned no rates for {Pair}.", provider.GetType().Name, symbol);
					continue;
				}
				result.AddRange(rates);
				HashSet<DateTime> receivedDates = rates.Select((ExternalMarketRate x) => x.RateDateUtc.Date).ToHashSet();
				remainingDates.ExceptWith(receivedDates);
				logger.LogInformation("Provider {Provider} returned {ReceivedCount} rates for {Pair}. Remaining missing dates: {RemainingCount}.", provider.GetType().Name, rates.Count, symbol, remainingDates.Count);
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				logger.LogWarning(ex2, "Provider {Provider} failed for {Pair}.", provider.GetType().Name, symbol);
			}
		}
		if (remainingDates.Count > 0)
		{
			logger.LogWarning("Could not obtain rates for {Pair} for {RemainingCount} dates: {Dates}.", symbol, remainingDates.Count, string.Join(", ", from x in remainingDates.Order()
				select x.ToString("yyyy-MM-dd")));
		}
		if (result.Count == 0)
		{
			throw new InvalidOperationException("Failed to fetch rates for " + symbol + " from any provider.");
		}
		return result;
	}

	/// <summary>
	/// Saves external market rates to the repository and returns the result of the refresh operation.
	/// </summary>
	/// <param name="rates">The collection of external market rates to save.</param>
	/// <param name="requestedCount">The number of rates that were requested.</param>
	/// <param name="providersUsed">The number of providers used to obtain the rates.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the market rate refresh result.</returns>
	private async Task<MarketRateRefreshResult> SaveRatesAsync(IReadOnlyCollection<ExternalMarketRate> rates, int requestedCount, int providersUsed, CancellationToken cancellationToken)
	{
		DateTime createdOn = DateTime.UtcNow;
		List<MarketRate> entities = rates.Select((ExternalMarketRate rate) => new MarketRate
		{
			Rate = rate.Rate,
			UpdatedAtUtc = rate.RateDateUtc,
			CurrencyPair = new CurrencyPair
			{
				BaseCurrency = rate.BaseCurrency,
				QuoteCurrency = rate.QuoteCurrency
			}
		}).ToList();
		int changedCount = await repository.UpsertRatesAsync(entities, cancellationToken);
		logger.LogInformation("Market rates refreshed. Requested: {RequestedCount}, received: {ReceivedCount}, saved: {SavedCount}, refreshedAtUtc: {RefreshedAtUtc}", requestedCount, rates.Count, changedCount, createdOn);
		return new MarketRateRefreshResult(providersUsed, changedCount, createdOn);
	}

	/// <summary>
	/// Parses a currency pair symbol in the format 'XXX/YYY' into a CurrencyPairRequest object.
	/// </summary>
	/// <param name="symbol">The currency pair symbol to parse, expected in the format 'XXX/YYY'.</param>
	/// <returns>A CurrencyPairRequest object if parsing is successful; otherwise, null.</returns>
	private static CurrencyPairRequest? ParseSymbol(string? symbol)
	{
		if (string.IsNullOrWhiteSpace(symbol))
		{
			return null;
		}
		string[] parts = symbol.Split('/', 2, StringSplitOptions.TrimEntries);
		if (parts.Length != 2 || parts[0].Length != 3 || parts[1].Length != 3)
		{
			return null;
		}
		return new CurrencyPairRequest(parts[0].ToUpperInvariant(), parts[1].ToUpperInvariant());
	}
}
