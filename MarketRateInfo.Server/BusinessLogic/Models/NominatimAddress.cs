using System.Text.Json.Serialization;

namespace MarketRateInfo.Server.BusinessLogic.Models;

public sealed class NominatimAddress
{
	[JsonPropertyName("city")]
	public string? City { get; set; }

	[JsonPropertyName("town")]
	public string? Town { get; set; }

	[JsonPropertyName("village")]
	public string? Village { get; set; }

	[JsonPropertyName("county")]
	public string? County { get; set; }
}
