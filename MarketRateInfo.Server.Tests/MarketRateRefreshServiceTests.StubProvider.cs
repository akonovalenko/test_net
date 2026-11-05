using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Interfaces;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The MarketRateRefreshServiceTests class contains unit tests for the MarketRateRefreshService,
/// </summary>
public sealed partial class MarketRateRefreshServiceTests
{
    /// <summary>
    /// The StubProvider class is a stub implementation of the IMarketRateProvider interface,
    /// </summary>
    /// <param name="rates">the list of external market rates to return</param>
    private sealed class StubProvider(IReadOnlyCollection<ExternalMarketRate> rates) : IMarketRateProvider
    {
        public int Calls { get; private set; }
        public (string, string)? LastPair { get; private set; }

        public bool Supports(string fromCurrency, string toCurrency) => true;

        public Task<IReadOnlyCollection<ExternalMarketRate>> GetRatesAsync(
            string fromCurrency, string toCurrency, IReadOnlyCollection<DateTime> dates,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastPair = (fromCurrency, toCurrency);
            return Task.FromResult(rates);
        }
    }
}
