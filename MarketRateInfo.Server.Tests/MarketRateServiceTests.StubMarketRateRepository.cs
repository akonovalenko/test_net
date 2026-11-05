using MarketRateInfo.Server.DataAccess.Entities;
using MarketRateInfo.Server.DataAccess.Repositories;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The MarketRateServiceTests class contains unit tests for the MarketRateService,
/// </summary>
public sealed partial class MarketRateServiceTests
{
    /// <summary>
    /// The StubMarketRateRepository class is a stub implementation of the IMarketRateRepository interface,
    /// </summary>
    private sealed class StubMarketRateRepository : IMarketRateRepository
    {
        public IReadOnlyList<MarketRate> Rates { get; init; } = [];
        public IReadOnlyList<MarketRate> History { get; init; } = [];
        public bool AddPairResult { get; init; } = true;
        public bool RemovePairResult { get; init; } = true;
        public string? LastHistorySymbol { get; private set; }
        public DateTime LastFromUtc { get; private set; }
        public DateTime LastToUtcExclusive { get; private set; }
        public (string, string)? LastAddedPair { get; private set; }
        public (string, string)? LastRemovedPair { get; private set; }

        public Task<IReadOnlyList<MarketRate>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Rates);

        public Task<MarketRate?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult<MarketRate?>(null);

        public Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<DateTime?> GetLatestRateDateAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult<DateTime?>(null);

        public Task<IReadOnlyList<MarketRate>> GetHistoryAsync(
            string symbol, DateTime fromUtc, DateTime toUtcExclusive,
            CancellationToken cancellationToken = default)
        {
            LastHistorySymbol = symbol;
            LastFromUtc = fromUtc;
            LastToUtcExclusive = toUtcExclusive;
            return Task.FromResult(History);
        }

        public Task<IReadOnlyList<DateTime>> GetMissingDatesAsync(
            string symbol, DateTime startDate, DateTime endDate,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DateTime>>([]);

        public Task<int> UpsertRatesAsync(
            IReadOnlyCollection<MarketRate> rates, CancellationToken cancellationToken = default)
            => Task.FromResult(rates.Count);

        public Task<bool> AddCurrencyPairAsync(
            string baseCurrency, string quoteCurrency, CancellationToken cancellationToken = default)
        {
            LastAddedPair = (baseCurrency, quoteCurrency);
            return Task.FromResult(AddPairResult);
        }

        public Task<bool> RemoveCurrencyPairAsync(
            string baseCurrency, string quoteCurrency, CancellationToken cancellationToken = default)
        {
            LastRemovedPair = (baseCurrency, quoteCurrency);
            return Task.FromResult(RemovePairResult);
        }
    }
}
