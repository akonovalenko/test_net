using MarketRateInfo.Server.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketRateInfo.Server.DataAccess.Repositories;

/// <summary>
/// Repository for managing market rates and currency pairs in the database.
/// </summary>
/// <param name="dbContext">The database context.</param>
public sealed class MarketRateRepository(MarketRateDbContext dbContext) : IMarketRateRepository
{
    public async Task<IReadOnlyList<MarketRate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MarketRates
            .AsNoTracking()
            .Include(x => x.CurrencyPair)
            .OrderBy(x => x.CurrencyPair.BaseCurrency)
            .ThenBy(x => x.CurrencyPair.QuoteCurrency)
            .ToListAsync(cancellationToken);
    }

    public Task<MarketRate?> GetBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSymbol(symbol, out var baseCurrency, out var quoteCurrency))
            return Task.FromResult<MarketRate?>(null);

        return dbContext.MarketRates
            .AsNoTracking()
            .Include(x => x.CurrencyPair)
            .SingleOrDefaultAsync(
                x => x.CurrencyPair.BaseCurrency == baseCurrency &&
                     x.CurrencyPair.QuoteCurrency == quoteCurrency,
                cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetSymbolsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CurrencyPairs
            .AsNoTracking()
            .Select(x => x.BaseCurrency + "/" + x.QuoteCurrency)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLatestRateDateAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSymbol(symbol, out var baseCurrency, out var quoteCurrency))
            return null;

        return await dbContext.MarketRates
            .AsNoTracking()
            .Where(x =>
                x.CurrencyPair.BaseCurrency == baseCurrency &&
                x.CurrencyPair.QuoteCurrency == quoteCurrency)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => (DateTime?)x.UpdatedAtUtc.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MarketRate>> GetHistoryAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSymbol(symbol, out var baseCurrency, out var quoteCurrency))
            return [];

        return await dbContext.MarketRates
            .AsNoTracking()
            .Include(x => x.CurrencyPair)
            .Where(x =>
                x.CurrencyPair.BaseCurrency == baseCurrency &&
                x.CurrencyPair.QuoteCurrency == quoteCurrency &&
                x.UpdatedAtUtc >= fromUtc &&
                x.UpdatedAtUtc < toUtcExclusive)
            .OrderBy(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DateTime>> GetMissingDatesAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseSymbol(symbol, out var baseCurrency, out var quoteCurrency))
            return [];

        var firstDate = startDate.Date;
        var lastDate = endDate.Date;

        if (firstDate > lastDate)
            throw new ArgumentException("startDate cannot be greater than endDate.");

        var existingDates = await dbContext.MarketRates
            .AsNoTracking()
            .Where(x =>
                x.CurrencyPair.BaseCurrency == baseCurrency &&
                x.CurrencyPair.QuoteCurrency == quoteCurrency &&
                x.UpdatedAtUtc >= firstDate &&
                x.UpdatedAtUtc < lastDate.AddDays(1))
            .Select(x => x.UpdatedAtUtc.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        var existingDateSet = existingDates.ToHashSet();
        var missingDates = new List<DateTime>();

        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            if (!existingDateSet.Contains(date))
                missingDates.Add(date);
        }

        return missingDates;
    }

    public async Task<bool> AddCurrencyPairAsync(
        string baseCurrency,
        string quoteCurrency,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.CurrencyPairs.AnyAsync(
            cp => cp.BaseCurrency == baseCurrency &&
                  cp.QuoteCurrency == quoteCurrency,
            cancellationToken);

        if (exists)
            return false;

        dbContext.CurrencyPairs.Add(new CurrencyPair
        {
            BaseCurrency = baseCurrency,
            QuoteCurrency = quoteCurrency
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveCurrencyPairAsync(
    string baseCurrency,
    string quoteCurrency,
    CancellationToken cancellationToken = default)
    {
        var pair = await dbContext.CurrencyPairs
            .FirstOrDefaultAsync(
                cp => cp.BaseCurrency == baseCurrency &&
                      cp.QuoteCurrency == quoteCurrency,
                cancellationToken);

        if (pair == null)
            return false;

        // Если есть зависимые данные (например, курсы, истории), их тоже нужно удалить
        // Допустим, у CurrencyPair есть навигационное свойство Rates
        if (pair.MarketRates != null && pair.MarketRates.Any())
        {
            dbContext.MarketRates.RemoveRange(pair.MarketRates);
        }

        dbContext.CurrencyPairs.Remove(pair);

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }


    public async Task<int> UpsertRatesAsync(
            IReadOnlyCollection<MarketRate> rates,
            CancellationToken cancellationToken = default)
    {
        if (rates.Count == 0)
            return 0;

        // Группируем по валютной паре И ДАТЕ (без учета времени), чтобы не плодить дубли за один день
        var incoming = rates
            .Select(x => new
            {
                BaseCurrency = x.CurrencyPair.BaseCurrency.Trim().ToUpperInvariant(),
                QuoteCurrency = x.CurrencyPair.QuoteCurrency.Trim().ToUpperInvariant(),
                Rate = decimal.Round(x.Rate, 8),
                UpdatedAtUtc = x.UpdatedAtUtc,
                RateDate = x.UpdatedAtUtc.Date
            })
            .GroupBy(x => new { Key = $"{x.BaseCurrency}/{x.QuoteCurrency}", x.RateDate })
            .Select(g => g.Last())
            .ToList();

        var pairs = await dbContext.CurrencyPairs.ToListAsync(cancellationToken);

        var pairMap = pairs.ToDictionary(
            x => $"{x.BaseCurrency}/{x.QuoteCurrency}",
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in incoming)
        {
            var key = $"{item.BaseCurrency}/{item.QuoteCurrency}";

            if (!pairMap.TryGetValue(key, out var pair))
            {
                pair = new CurrencyPair
                {
                    BaseCurrency = item.BaseCurrency,
                    QuoteCurrency = item.QuoteCurrency
                };

                dbContext.CurrencyPairs.Add(pair);
                pairMap[key] = pair;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var pairIds = incoming
            .Select(x => pairMap[$"{x.BaseCurrency}/{x.QuoteCurrency}"].Id)
            .Distinct()
            .ToList();

        var incomingDates = incoming.Select(x => x.RateDate).Distinct().ToList();

        // Загружаем только существующие записи на совпадающие даты
        var existingRates = await dbContext.MarketRates
            .Where(x => pairIds.Contains(x.CurrencyPairId) && incomingDates.Contains(x.UpdatedAtUtc.Date))
            .ToListAsync(cancellationToken);

        var existingMap = existingRates
            .GroupBy(x => new { x.CurrencyPairId, Date = x.UpdatedAtUtc.Date })
            .ToDictionary(g => g.Key, g => g.First());

        var changedCount = 0;

        foreach (var item in incoming)
        {
            var pair = pairMap[$"{item.BaseCurrency}/{item.QuoteCurrency}"];
            var lookupKey = new { CurrencyPairId = pair.Id, Date = item.RateDate };

            if (existingMap.TryGetValue(lookupKey, out var existing))
            {
                existing.Rate = item.Rate;
                existing.UpdatedAtUtc = item.UpdatedAtUtc;
                changedCount++;
            }
            else
            {
                dbContext.MarketRates.Add(new MarketRate
                {
                    CurrencyPairId = pair.Id,
                    Rate = item.Rate,
                    UpdatedAtUtc = item.UpdatedAtUtc
                });

                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return changedCount;
    }

    private static bool TryParseSymbol(
        string symbol,
        out string baseCurrency,
        out string quoteCurrency)
    {
        baseCurrency = string.Empty;
        quoteCurrency = string.Empty;

        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        var parts = symbol.Split('/', 2, StringSplitOptions.TrimEntries);

        if (parts.Length != 2 ||
            parts[0].Length != 3 ||
            parts[1].Length != 3)
            return false;

        baseCurrency = parts[0].ToUpperInvariant();
        quoteCurrency = parts[1].ToUpperInvariant();
        return true;
    }
}
