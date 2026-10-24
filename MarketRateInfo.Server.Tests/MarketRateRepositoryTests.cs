using MarketRateInfo.Server.DataAccess;
using MarketRateInfo.Server.DataAccess.Entities;
using MarketRateInfo.Server.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MarketRateInfo.Server.Tests;

public sealed class MarketRateRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MarketRateDbContext _dbContext;
    private readonly MarketRateRepository _repository;

    public MarketRateRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MarketRateDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new MarketRateDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new MarketRateRepository(_dbContext);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRatesOrderedBySymbol()
    {
        _dbContext.MarketRates.AddRange(
            new MarketRate
            {
                CurrencyPair = new CurrencyPair { BaseCurrency = "USD", QuoteCurrency = "UAH" },
                Rate = 41.25m,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new MarketRate
            {
                CurrencyPair = new CurrencyPair { BaseCurrency = "EUR", QuoteCurrency = "USD" },
                Rate = 1.17m,
                UpdatedAtUtc = DateTime.UtcNow
            });

        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("EUR/USD", result[0].CurrencyPair.BaseCurrency + "/" + result[0].CurrencyPair.QuoteCurrency);
        Assert.Equal("USD/UAH", result[1].CurrencyPair.BaseCurrency + "/" + result[1].CurrencyPair.QuoteCurrency);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsMatchingRate()
    {
        var expected = new MarketRate
        {
            CurrencyPair = new CurrencyPair { BaseCurrency = "EUR", QuoteCurrency = "USD" },
            Rate = 1.17m,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.MarketRates.Add(expected);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetBySymbolAsync("EUR/USD");

        Assert.NotNull(result);
        Assert.Equal(expected.CurrencyPair, result.CurrencyPair);
        Assert.Equal(expected.Rate, result.Rate);
    }

    [Fact]
    public async Task GetBySymbolAsync_ReturnsNullForUnknownSymbol()
    {
        var result = await _repository.GetBySymbolAsync("GBP/USD");

        Assert.Null(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
