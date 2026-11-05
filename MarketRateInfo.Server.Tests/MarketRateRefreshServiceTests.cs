using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The MarketRateRefreshServiceTests class contains unit tests for the MarketRateRefreshService, 
/// which is responsible for refreshing market rate data by fetching missing rates 
/// from external providers and persisting them to the repository. 
/// The tests verify that the service correctly handles various scenarios, 
/// including fetching missing dates, handling invalid symbols, and using multiple providers when necessary.
/// </summary>
public sealed partial class MarketRateRefreshServiceTests
{
    [Fact]
    public async Task RefreshAsync_ForSymbol_FetchesMissingDatesAndPersistsRates()
    {
        var date = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var repository = new StubRepository { MissingDates = [date] };
        var provider = new StubProvider(
            [new ExternalMarketRate("EUR", "USD", 1.17m, date)]);
        var service = new MarketRateRefreshService(
            repository, [provider], NullLogger<MarketRateRefreshService>.Instance);

        var result = await service.RefreshAsync("eur/usd", date, date);

        Assert.Equal(1, result.ProvidersUsed);
        Assert.Equal(1, result.RatesSaved);
        Assert.Equal(1, provider.Calls);
        Assert.Equal(("EUR", "USD"), provider.LastPair!.Value);
        Assert.Single(repository.SavedRates);
        Assert.Equal(1.17m, repository.SavedRates.Single().Rate);
    }

    [Fact]
    public async Task RefreshAsync_ThrowsForInvalidSymbol()
    {
        var service = new MarketRateRefreshService(
            new StubRepository(), [], NullLogger<MarketRateRefreshService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RefreshAsync("EUR", DateTime.UtcNow.Date, DateTime.UtcNow.Date));
    }

    [Fact]
    public async Task RefreshAsync_ThrowsWhenNoProvidersConfigured()
    {
        var service = new MarketRateRefreshService(
            new StubRepository { MissingDates = [DateTime.UtcNow.Date] },
            [], NullLogger<MarketRateRefreshService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshAsync("EUR/USD", DateTime.UtcNow.Date, DateTime.UtcNow.Date));
    }

    [Fact]
    public async Task RefreshAsync_SkipsProviderWhenNoDatesAreMissing()
    {
        var provider = new StubProvider([]);
        var repository = new StubRepository { MissingDates = [] };
        var service = new MarketRateRefreshService(
            repository, [provider], NullLogger<MarketRateRefreshService>.Instance);

        var result = await service.RefreshAsync(
            "EUR/USD", DateTime.UtcNow.Date, DateTime.UtcNow.Date);

        Assert.Equal(0, result.RatesSaved);
        Assert.Equal(0, provider.Calls);
        Assert.Empty(repository.SavedRates);
    }

    [Fact]
    public async Task RefreshAsync_UsesNextProviderForDatesNotReturnedByFirstProvider()
    {
        var firstDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var secondDate = firstDate.AddDays(1);
        var repository = new StubRepository { MissingDates = [firstDate, secondDate] };
        var first = new StubProvider(
            [new ExternalMarketRate("EUR", "USD", 1.1m, firstDate)]);
        var second = new StubProvider(
            [new ExternalMarketRate("EUR", "USD", 1.2m, secondDate)]);

        var service = new MarketRateRefreshService(
            repository, [first, second], NullLogger<MarketRateRefreshService>.Instance);

        var result = await service.RefreshAsync("EUR/USD", firstDate, secondDate);

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.Equal(2, result.RatesSaved);
        Assert.Equal(2, repository.SavedRates.Count);
    }
}
