using MarketRateInfo.Server.BusinessLogic.Models.Responses;

namespace MarketRateInfo.Server.BusinessLogic.Interfaces;

/// <summary>
/// Defines operations for retrieving market rates, managing currency pairs, and accessing historical rate data.
/// </summary>
public interface IMarketRateService
{
	/// <summary>
	/// Asynchronously retrieves the home view model.
	/// </summary>
	/// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the home response view model.</returns>
	Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default(CancellationToken));

	/// <summary>
	/// Asynchronously retrieves a read-only list of symbol names.
	/// </summary>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation, containing a read-only list of symbol names.</returns>
	Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken cancellationToken = default(CancellationToken));

	/// <summary>
	/// Retrieves historical market rates for the specified symbol within the given date range.
	/// </summary>
	/// <param name="symbol">Market symbol to retrieve history for.</param>
	/// <param name="from">Start date of the history range.</param>
	/// <param name="to">End date of the history range.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A read-only list of market rate responses for the specified period.</returns>
	Task<IReadOnlyList<MarketRateResponse>> GetHistoryAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default(CancellationToken));

	/// <summary>
	/// Adds a new currency pair asynchronously.
	/// </summary>
	/// <param name="symbol">The symbol representing the currency pair to add.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task AddCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default(CancellationToken));

	/// <summary>
	/// Removes a currency pair identified by its symbol asynchronously.
	/// </summary>
	/// <param name="symbol">The symbol of the currency pair to remove.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous remove operation.</returns>
	Task RemoveCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default(CancellationToken));
}
