using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The HomeControllerTests class contains unit tests for the HomeController, which is responsible for handling
/// requests related to the home endpoint of the Market Rate Info application. 
/// The tests verify that the controller correctly returns service responses and forwards cancellation tokens 
/// to the underlying service layer.
/// </summary>
public sealed partial class HomeControllerTests
{
    /// <summary>
    /// The StubMarketRateService class is a stub implementation of the IMarketRateService interface,
    /// </summary>
    private sealed class StubMarketRateService : IMarketRateService
    {
        public HomeResponse HomeResponse { get; set; } =
            new("Market Rate Info", "ok", "SQLite", []);

        public int GetHomeCalls { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<HomeResponse> GetHomeAsync(CancellationToken cancellationToken = default)
        {
            GetHomeCalls++;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(HomeResponse);
        }

        public Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<MarketRateResponse>> GetHistoryAsync(
            string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MarketRateResponse>>([]);

        public Task AddCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
