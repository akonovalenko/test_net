using MarketRateInfo.Server.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketRateInfo.Server.DataAccess;

/// <summary>
/// Applies EF Core migrations and seeds the application's default currency pairs.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<MarketRateDbContext>();

        // Creates the SQLite database when absent and applies pending migrations.
        await db.Database.MigrateAsync(cancellationToken);

        await EnsureDefaultCurrencyPairsAsync(db, cancellationToken);
    }

    private static async Task EnsureDefaultCurrencyPairsAsync(
        MarketRateDbContext db,
        CancellationToken cancellationToken)
    {
        var defaults = new[]
        {
            (Base: "EUR", Quote: "USD"),
            (Base: "USD", Quote: "UAH"),
            (Base: "BTC", Quote: "USD"),
            (Base: "ETH", Quote: "USD")
        };

        foreach (var item in defaults)
        {
            var exists = await db.CurrencyPairs.AnyAsync(
                x => x.BaseCurrency == item.Base && x.QuoteCurrency == item.Quote,
                cancellationToken);

            if (!exists)
            {
                db.CurrencyPairs.Add(new CurrencyPair
                {
                    BaseCurrency = item.Base,
                    QuoteCurrency = item.Quote
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
