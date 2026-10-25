using System;

namespace MarketRateInfo.Server.BusinessLogic.Models;

public class WeatherForecast
{
	public DateOnly Date { get; set; }

	public int TemperatureC { get; set; }

	public string? Summary { get; set; }

	public string CityName { get; set; } = "Kyiv";

	public double WindSpeed { get; set; }

	public int WindDirection { get; set; }

	public int Humidity { get; set; }
}
