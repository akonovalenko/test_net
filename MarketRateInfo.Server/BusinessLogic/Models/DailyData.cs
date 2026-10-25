using System.Text.Json.Serialization;

namespace MarketRateInfo.Server.BusinessLogic.Models;

public sealed class DailyData
{
	[JsonPropertyName("time")]
	public List<string>? Time { get; set; }

	[JsonPropertyName("temperature_2m_max")]
	public List<double>? Temperature2mMax { get; set; }

	[JsonPropertyName("wind_speed_10m_max")]
	public List<double>? WindSpeed10mMax { get; set; }

	[JsonPropertyName("wind_direction_10m_dominant")]
	public List<double>? WindDirection10mDominant { get; set; }

	[JsonPropertyName("relative_humidity_2m_max")]
	public List<double>? RelativeHumidity2mMax { get; set; }
}
