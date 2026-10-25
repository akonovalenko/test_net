namespace MarketRateInfo.Server.BusinessLogic.Models;

internal sealed record MarketRatePairRefreshResult(string Symbol, int RatesSaved, bool Skipped, bool Failed);
