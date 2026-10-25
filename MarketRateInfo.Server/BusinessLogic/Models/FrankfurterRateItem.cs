using System.Text.Json.Serialization;

namespace MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;

public sealed record FrankfurterRateItem([property: JsonPropertyName("date")] string Date, [property: JsonPropertyName("base")] string Base, [property: JsonPropertyName("quote")] string Quote, [property: JsonPropertyName("rate")] decimal Rate);
