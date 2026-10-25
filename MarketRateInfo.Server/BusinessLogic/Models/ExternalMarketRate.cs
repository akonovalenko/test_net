using System;

namespace MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;

public sealed record ExternalMarketRate(string BaseCurrency, string QuoteCurrency, decimal Rate, DateTime RateDateUtc);
