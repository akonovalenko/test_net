using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;

namespace MarketRateInfo.Server.BusinessLogic.Interfaces;

/// <summary>
/// Provides methods for retrieving external market rates between currency pairs.
/// </summary>
public interface IMarketRateProvider
{
	/// <summary>
	/// Retrieves external market rates for the specified currency pair and dates.
	/// </summary>
	/// <param name="fromCurrency">The ISO code of the source currency.</param>
	/// <param name="toCurrency">The ISO code of the target currency.</param>
	/// <param name="dates">The collection of dates for which to retrieve rates.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A read-only collection of external market rates.</returns>
	Task<IReadOnlyCollection<ExternalMarketRate>> GetRatesAsync(string fromCurrency, string toCurrency, IReadOnlyCollection<DateTime> dates, CancellationToken cancellationToken = default(CancellationToken));

	/// <summary>
	/// Determines whether conversion between the specified currencies is supported.
	/// </summary>
	/// <param name="fromCurrency">The ISO code of the source currency.</param>
	/// <param name="toCurrency">The ISO code of the target currency.</param>
	/// <returns>true if the conversion is supported; otherwise, false.</returns>
	bool Supports(string fromCurrency, string toCurrency);
}
