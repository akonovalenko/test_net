using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models;
using MarketRateInfo.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MarketRateInfo.Server.Tests;

public sealed class HomeControllerTests
{
    [Fact]
    public async Task Get_ReturnsOkWithRates()
    {
        var service = new StubMarketRateService(
            new HomeResponse(
                Application: "Market Rate Info",
                Status: "ok",
                Database: "SQLite",
                Rates:
                [
                    new HomeRateResponse(
                        Symbol: "EUR/USD",
                        Rate: 1.17m,
                        UpdatedAtUtc: DateTime.UtcNow)
                ]));

        var controller = new HomeController(service);

        var result = await controller.Get(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<HomeResponse>(okResult.Value);

        Assert.Equal("Market Rate Info", response.Application);
        Assert.Equal("ok", response.Status);
        Assert.Single(response.Rates);
        Assert.Equal("EUR/USD", response.Rates[0].Symbol);
        Assert.Equal(1.17m, response.Rates[0].Rate);
    }

    private sealed class StubMarketRateService(
        HomeResponse response) : IMarketRateService
    {
        public Task<HomeResponse> GetHomeAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<string>> GetSymbolsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<MarketRateResponse>> GetHistoryAsync(
            string symbol,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MarketRateResponse>>([]);
        }

        // Implemented to satisfy the current IMarketRateService interface.
        public Task AddCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // Stub: do nothing in tests.
            return Task.CompletedTask;
        }

        public Task RemoveCurrencyPairAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}