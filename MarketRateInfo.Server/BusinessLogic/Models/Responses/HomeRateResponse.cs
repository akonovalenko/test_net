using System;

namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed record HomeRateResponse(string Symbol, decimal Rate, DateTime UpdatedAtUtc);
