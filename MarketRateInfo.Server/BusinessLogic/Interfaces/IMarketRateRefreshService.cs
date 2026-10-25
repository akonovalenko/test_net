using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;

namespace MarketRateInfo.Server.BusinessLogic.Interfaces;

/// <summary>
/// Defines methods for refreshing market rates over a specified date range, optionally filtered by symbol.
/// </summary>
public interface IMarketRateRefreshService
{
	/// <summary>
	/// Asynchronously refreshes market rates within the specified date range.
	/// </summary>
	/// <param name="startDate">The start date of the range to refresh market rates for.</param>
	/// <param name="endDate">The end date of the range to refresh market rates for.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the market rate refresh result.</returns>
	Task<MarketRateRefreshResult> RefreshAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default(CancellationToken));

	/// <summary>
	/// Asynchronously refreshes market rates for the specified symbol within the given date range.
	/// </summary>
	/// <param name="symbol">The market symbol to refresh rates for.</param>
	/// <param name="startDate">The start date of the range to refresh.</param>
	/// <param name="endDate">The end date of the range to refresh.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the market rate refresh result.</returns>
	Task<MarketRateRefreshResult> RefreshAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default(CancellationToken));
}
