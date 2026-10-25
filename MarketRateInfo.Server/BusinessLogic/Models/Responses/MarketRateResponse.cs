using System;

namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed record MarketRateResponse(string Symbol, decimal Rate, DateTime UpdatedAtUtc);
