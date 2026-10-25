using System.Text.Json.Serialization;

namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed class OpenMeteoResponse
{
	[JsonPropertyName("daily")]
	public DailyData? Daily { get; set; }
}
