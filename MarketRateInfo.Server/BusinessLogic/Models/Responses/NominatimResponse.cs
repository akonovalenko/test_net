using System.Text.Json.Serialization;

namespace MarketRateInfo.Server.BusinessLogic.Models.Responses;

public sealed class NominatimResponse
{
	[JsonPropertyName("address")]
	public NominatimAddress? Address { get; set; }
}
