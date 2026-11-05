using MarketRateInfo.Server.BusinessLogic.Exceptions;
using MarketRateInfo.Server.BusinessLogic.Services;
using MarketRateInfo.Server.DataAccess.Entities;
using Xunit;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The MarketRateServiceTests class contains unit tests for the MarketRateService,
/// </summary>
public sealed partial class MarketRateServiceTests
{
    [Fact]
    public async Task GetHomeAsync_MapsRatesAndApplicationMetadata()
    {
        var repository = new StubMarketRateRepository
        {
            Rates =
            [
                NewRate("EUR", "USD", 1.17m, Utc(2026, 1, 2))
            ]
        };

        var response = await new MarketRateService(repository).GetHomeAsync();

        Assert.Equal("Market Rate Info", response.Application);
        Assert.Equal("ok", response.Status);
        Assert.Equal("SQLite", response.Database);
        var rate = Assert.Single(response.Rates);
        Assert.Equal("EUR/USD", rate.Symbol);
        Assert.Equal(1.17m, rate.Rate);
    }

    [Fact]
    public async Task GetHistoryAsync_NormalizesSymbolAndMakesEndDateInclusive()
    {
        var repository = new StubMarketRateRepository
        {
            History =
            [
                NewRate("EUR", "USD", 1.2m, Utc(2026, 1, 2))
            ]
        };

        var result = await new MarketRateService(repository).GetHistoryAsync(
            " eur/usd ",
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 2));

        Assert.Equal("EUR/USD", repository.LastHistorySymbol);
        Assert.Equal(Utc(2026, 1, 1), repository.LastFromUtc);
        Assert.Equal(Utc(2026, 1, 3), repository.LastToUtcExclusive);
        Assert.Single(result);
        Assert.Equal("EUR/USD", result[0].Symbol);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetHistoryAsync_RejectsMissingSymbol(string? symbol)
    {
        var service = new MarketRateService(new StubMarketRateRepository());

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.GetHistoryAsync(symbol!, Utc(2026, 1, 1), Utc(2026, 1, 2)));

        Assert.Contains("required", exception.Message);
    }

    [Fact]
    public async Task GetHistoryAsync_RejectsReversedDateRange()
    {
        var service = new MarketRateService(new StubMarketRateRepository());

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.GetHistoryAsync("EUR/USD", Utc(2026, 1, 2), Utc(2026, 1, 1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("EUR")]
    [InlineData("EURO/USD")]
    [InlineData("EUR/US")]
    public async Task AddCurrencyPairAsync_RejectsInvalidSymbol(string symbol)
    {
        var service = new MarketRateService(new StubMarketRateRepository());

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.AddCurrencyPairAsync(symbol));
    }

    [Fact]
    public async Task AddCurrencyPairAsync_NormalizesSymbolBeforeSaving()
    {
        var repository = new StubMarketRateRepository();
        await new MarketRateService(repository).AddCurrencyPairAsync(" eur / usd ");

        Assert.Equal(("EUR", "USD"), repository.LastAddedPair!.Value);
    }

    [Fact]
    public async Task AddCurrencyPairAsync_RejectsDuplicatePair()
    {
        var repository = new StubMarketRateRepository { AddPairResult = false };
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            new MarketRateService(repository).AddCurrencyPairAsync("EUR/USD"));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task RemoveCurrencyPairAsync_RejectsPairNotFound()
    {
        var repository = new StubMarketRateRepository { RemovePairResult = false };

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            new MarketRateService(repository).RemoveCurrencyPairAsync("EUR/USD"));
    }

    [Fact]
    public async Task RemoveCurrencyPairAsync_NormalizesSymbolBeforeDeleting()
    {
        var repository = new StubMarketRateRepository();
        await new MarketRateService(repository).RemoveCurrencyPairAsync(" eur/usd ");

        Assert.Equal(("EUR", "USD"), repository.LastRemovedPair!.Value);
    }

    private static MarketRate NewRate(string from, string to, decimal rate, DateTime date)
        => new()
        {
            CurrencyPair = new CurrencyPair { BaseCurrency = from, QuoteCurrency = to },
            Rate = rate,
            UpdatedAtUtc = date
        };

    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
