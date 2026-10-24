namespace MarketRateInfo.Server.DataAccess.Entities;

/// <summary>
/// Represents a market rate entity in the database, which stores the exchange rate for a specific currency pair at a given point in time.
/// </summary>
public sealed class MarketRate
{
    public int Id { get; set; }
    public int CurrencyPairId { get; set; }
    public CurrencyPair CurrencyPair { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
