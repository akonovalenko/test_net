using MarketRateInfo.Server.BusinessLogic.Exceptions;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;
using MarketRateInfo.Server.DataAccess.Entities;
using MarketRateInfo.Server.DataAccess.Repositories;
using System.Collections.ObjectModel;

namespace MarketRateInfo.Server.BusinessLogic.Services;

/// <summary>
/// Provides operations for managing market rates and currency pairs.
/// </summary>
/// <param name="marketRateRepository">The repository used for accessing market rate data.</param>
public sealed class MarketRateService(IMarketRateRepository marketRateRepository) : IMarketRateService
{
	/// <summary>
	/// Retrieves home information including market rate data asynchronously.
	/// </summary>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a HomeResponse with market rate
	/// information.</returns>
	public async Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ReadOnlyCollection<HomeRateResponse> response = (await marketRateRepository.GetAllAsync(cancellationToken)).Select(MapToHomeRate).ToList().AsReadOnly();
		return new HomeResponse("Market Rate Info", "ok", "SQLite", response);
	}

	/// <summary>
	/// Asynchronously retrieves a read-only list of available market symbols.
	/// </summary>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation, containing a read-only list of market symbol strings.</returns>
	public Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return marketRateRepository.GetSymbolsAsync(cancellationToken);
	}

	/// <summary>
	/// Retrieves historical market rates for the specified symbol within the given date range.
	/// </summary>
	/// <param name="symbol">Market symbol to retrieve history for.</param>
	/// <param name="from">Start date of the history range, inclusive.</param>
	/// <param name="to">End date of the history range, inclusive.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A read-only list of market rate responses within the specified date range.</returns>
	public async Task<IReadOnlyList<MarketRateResponse>> GetHistoryAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateHistoryRequest(symbol, from, to);
		string normalizedSymbol = NormalizeSymbol(symbol);
		DateTime fromUtc = ((from.Kind == DateTimeKind.Utc) ? from.Date : DateTime.SpecifyKind(from.Date, DateTimeKind.Utc));
		DateTime toUtcExclusive = ((to.Kind == DateTimeKind.Utc) ? to.Date.AddDays(1.0) : DateTime.SpecifyKind(to.Date.AddDays(1.0), DateTimeKind.Utc));
		return (await marketRateRepository.GetHistoryAsync(normalizedSymbol, fromUtc, toUtcExclusive, cancellationToken)).Select(MapToResponse).ToList().AsReadOnly();
	}

	/// <summary>
	/// Adds a new currency pair to the repository asynchronously.
	/// </summary>
	/// <param name="symbol">Currency pair symbol in the format 'USD/EUR' using 3-letter currency codes.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="BusinessValidationException">Thrown when the symbol is invalid or the currency pair already exists.</exception>
	public async Task AddCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(symbol))
		{
			throw new BusinessValidationException("Symbol is required.");
		}
		string[] parts = symbol.Split('/', 2, StringSplitOptions.TrimEntries);
		if (parts.Length != 2 || parts[0].Length != 3 || parts[1].Length != 3)
		{
			throw new BusinessValidationException("Symbol format must be 'USD/EUR' (3-letter currency codes).");
		}
		string baseCurrency = parts[0].ToUpperInvariant();
		string quoteCurrency = parts[1].ToUpperInvariant();
		if (!(await marketRateRepository.AddCurrencyPairAsync(baseCurrency, quoteCurrency, cancellationToken)))
		{
			throw new BusinessValidationException($"Currency pair '{baseCurrency}/{quoteCurrency}' already exists.");
		}
	}

	/// <summary>
	/// Removes a currency pair from the repository asynchronously.
	/// </summary>
	/// <param name="symbol">The currency pair symbol in the format 'USD/EUR'.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	/// <exception cref="BusinessValidationException">Thrown when the symbol is invalid or the currency pair does not exist or cannot be removed.</exception>
	public async Task RemoveCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(symbol))
		{
			throw new BusinessValidationException("Symbol is required.");
		}
		string[] parts = symbol.Split('/', 2, StringSplitOptions.TrimEntries);
		if (parts.Length != 2 || parts[0].Length != 3 || parts[1].Length != 3)
		{
			throw new BusinessValidationException("Symbol format must be 'USD/EUR' (3-letter currency codes).");
		}
		string baseCurrency = parts[0].ToUpperInvariant();
		string quoteCurrency = parts[1].ToUpperInvariant();
		if (!(await marketRateRepository.RemoveCurrencyPairAsync(baseCurrency, quoteCurrency, cancellationToken)))
		{
			throw new BusinessValidationException($"Currency pair '{baseCurrency}/{quoteCurrency}' does not exist or cannot be removed.");
		}
	}

	/// <summary>
	/// Validates the parameters for a history request.
	/// </summary>
	/// <param name="symbol">The symbol to validate.</param>
	/// <param name="from">The start date of the history range.</param>
	/// <param name="to">The end date of the history range.</param>
	/// <exception cref="BusinessValidationException">Thrown when the symbol is null or whitespace, or when the from date is later than the to date.</exception>
	private static void ValidateHistoryRequest(string? symbol, DateTime from, DateTime to)
	{
		if (string.IsNullOrWhiteSpace(symbol))
		{
			throw new BusinessValidationException("The 'symbol' parameter is required.");
		}
		if (from > to)
		{
			throw new BusinessValidationException("The 'from' date must be earlier than or equal to the 'to' date.");
		}
	}

	/// <summary>
	/// Normalizes the specified symbol by trimming whitespace and converting to uppercase using the invariant culture.
	/// </summary>
	/// <param name="symbol">The symbol to normalize.</param>
	/// <returns>A normalized symbol string.</returns>
	private static string NormalizeSymbol(string symbol)
	{
		return symbol.Trim().ToUpperInvariant();
	}

	/// <summary>
	/// Maps a MarketRate entity to a HomeRateResponse DTO.
	/// </summary>
	/// <param name="rate">The MarketRate entity to map.</param>
	/// <returns>A HomeRateResponse DTO containing the mapped data.</returns>
	private static HomeRateResponse MapToHomeRate(MarketRate rate)
	{
		return new HomeRateResponse(BuildSymbol(rate), rate.Rate, rate.UpdatedAtUtc);
	}

	/// <summary>
	/// Creates a MarketRateResponse from a MarketRate entity.
	/// </summary>
	/// <param name="rate">The MarketRate entity to map.</param>
	/// <returns>A MarketRateResponse containing the mapped data.</returns>
	private static MarketRateResponse MapToResponse(MarketRate rate)
	{
		return new MarketRateResponse(BuildSymbol(rate), rate.Rate, rate.UpdatedAtUtc);
	}

	/// <summary>
	/// Builds a currency pair symbol from the specified market rate.
	/// </summary>
	/// <param name="rate">The market rate containing currency pair information.</param>
	/// <returns>A string representing the currency pair symbol, or "UNKNOWN/PAIR" if unavailable.</returns>
	private static string BuildSymbol(MarketRate rate)
	{
		if (rate.CurrencyPair != null)
		{
			return rate.CurrencyPair.BaseCurrency + "/" + rate.CurrencyPair.QuoteCurrency;
		}
		return "UNKNOWN/PAIR";
	}
}
