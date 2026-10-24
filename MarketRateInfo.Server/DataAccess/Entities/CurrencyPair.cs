namespace MarketRateInfo.Server.DataAccess.Entities
{
    /// <summary>
    /// Represents a currency pair entity in the database.
    /// </summary>
    public class CurrencyPair
    {
        public int Id { get; set; }
        public string BaseCurrency { get; set; } = null!;
        public string QuoteCurrency { get; set; } = null!;
        public ICollection<MarketRate> MarketRates { get; set; } = new List<MarketRate>();
    }
}