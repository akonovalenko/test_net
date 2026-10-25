using System.Collections.Generic;

namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed record HomeResponse(string Application, string Status, string Database, IReadOnlyList<HomeRateResponse> Rates);
