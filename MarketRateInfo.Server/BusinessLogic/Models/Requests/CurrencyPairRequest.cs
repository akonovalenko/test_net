namespace MarketRateInfo.Server.BusinessLogic.Models.Requests;

public sealed record CurrencyPairRequest(string BaseCurrency, string QuoteCurrency);
