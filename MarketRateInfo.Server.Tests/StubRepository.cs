using MarketRateInfo.Server.DataAccess.Entities;
using MarketRateInfo.Server.DataAccess.Repositories;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The MarketRateRefreshServiceTests class contains unit tests for the MarketRateRefreshService,
/// </summary>
public sealed partial class MarketRateRefreshServiceTests
{
    /// <summary>
    /// The StubRepository class is a stub implementation of the IMarketRateRepository interface,
    /// </summary>
    private sealed class StubRepository : IMarketRateRepository
    {
        public IReadOnlyList<DateTime> MissingDates { get; init; } = [];
        public IReadOnlyCollection<MarketRate> SavedRates { get; private set; } = [];

        public Task<IReadOnlyList<MarketRate>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MarketRate>>([]);

        public Task<MarketRate?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult<MarketRate?>(null);

        public Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["EUR/USD"]);

        public Task<DateTime?> GetLatestRateDateAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult<DateTime?>(null);

        public Task<IReadOnlyList<MarketRate>> GetHistoryAsync(
            string symbol, DateTime fromUtc, DateTime toUtcExclusive,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MarketRate>>([]);

        public Task<IReadOnlyList<DateTime>> GetMissingDatesAsync(
            string symbol, DateTime startDate, DateTime endDate,
            CancellationToken cancellationToken = default)
            => Task.FromResult(MissingDates);

        public Task<int> UpsertRatesAsync(
            IReadOnlyCollection<MarketRate> rates, CancellationToken cancellationToken = default)
        {
            SavedRates = rates.ToArray();
            return Task.FromResult(SavedRates.Count);
        }

        public Task<bool> AddCurrencyPairAsync(
            string baseCurrency, string quoteCurrency, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> RemoveCurrencyPairAsync(
            string baseCurrency, string quoteCurrency, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
