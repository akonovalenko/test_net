using System;

namespace MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;

public sealed record MarketRateRefreshResult(int ProvidersUsed, int RatesSaved, DateTime RefreshedAtUtc);
