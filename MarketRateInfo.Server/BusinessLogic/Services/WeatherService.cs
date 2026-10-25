using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;

namespace MarketRateInfo.Server.BusinessLogic.Services;

/// <summary>
/// Provides weather forecast data by consuming external weather and geolocation APIs.
/// </summary>
public class WeatherService : IWeatherService
{
	private readonly HttpClient _httpClient;

	private readonly ILogger<WeatherService> _logger;

	/// <summary>
	/// Initializes a new instance of the WeatherService class.
	/// </summary>
	/// <param name="httpClient">The HTTP client used for making API requests.</param>
	/// <param name="logger">The logger used for logging operations.</param>
	public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
	{
		this._httpClient = httpClient;
		this._logger = logger;
	}

	/// <summary>
	/// Retrieves a weather forecast for the specified geographic coordinates.
	/// </summary>
	/// <param name="latitude">Latitude of the location.</param>
	/// <param name="longitude">Longitude of the location.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A collection of weather forecast results for the given location.</returns>
	public async Task<IEnumerable<WeatherForecast>> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			OpenMeteoResponse response = await HttpClientJsonExtensions.GetFromJsonAsync<OpenMeteoResponse>(requestUri: $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&daily=temperature_2m_max,wind_speed_10m_max,wind_direction_10m_dominant,relative_humidity_2m_max&timezone=auto", client: _httpClient, cancellationToken: cancellationToken);
			string cityName = await GetCityNameAsync(latitude, longitude, cancellationToken);
			if (response?.Daily?.Time != null && response.Daily.Temperature2mMax != null)
			{
				List<WeatherForecast> list = new List<WeatherForecast>();
				for (int i = 0; i < response.Daily.Time.Count; i++)
				{
					string dateStr = response.Daily.Time[i];
					int tempC = (int)Math.Round(response.Daily.Temperature2mMax[i]);
					list.Add(new WeatherForecast
					{
						Date = DateOnly.Parse(dateStr),
						TemperatureC = tempC,
						Summary = GetSummaryByTemperature(tempC),
						WindSpeed = (response.Daily.WindSpeed10mMax?[i] ?? 0.0),
						WindDirection = (int)(response.Daily.WindDirection10mDominant?[i] ?? 0.0),
						Humidity = (int)(response.Daily.RelativeHumidity2mMax?[i] ?? 0.0),
						CityName = cityName
					});
				}
				return list;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_logger.LogError(ex2, "Failed to fetch weather from external API for coordinates {@Latitude}, {@Longitude}", latitude, longitude);
		}
		return from index in Enumerable.Range(1, 5)
			select new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = 20,
				Summary = "Unavailable (Fallback)",
				CityName = "Unknown"
			};
	}

	/// <summary>
	/// Returns a weather summary string based on the specified temperature in Celsius.
	/// </summary>
	/// <param name="tempC">The temperature in degrees Celsius.</param>
	/// <returns>A string representing the weather summary for the given temperature.</returns>
	private static string GetSummaryByTemperature(int tempC)
	{
		if (1 == 0)
		{
		}
		string result;
		if (tempC < 20)
		{
			if (tempC < 0)
			{
				result = "Freezing";
			}
			else
			{
				result = ((tempC >= 10) ? "Mild" : "Chilly");
			}
		}
		else
		{
			result = ((tempC >= 30) ? "Hot" : "Warm");
		}
		if (1 == 0)
		{
		}
		return result;
	}

	/// <summary>
	/// Retrieves the city name for the specified latitude and longitude using the Nominatim OpenStreetMap API.
	/// </summary>
	/// <param name="latitude">The latitude coordinate.</param>
	/// <param name="longitude">The longitude coordinate.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the resolved city name, or "Unknown
	/// Location" if not found.</returns>
	private async Task<string> GetCityNameAsync(
		double latitude, 
		double longitude, 
		CancellationToken cancellationToken)
	{
		try
		{
			using HttpRequestMessage requestMessage = new HttpRequestMessage(requestUri: $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=10", method: HttpMethod.Get);
			requestMessage.Headers.Add("User-Agent", "MarketRateInfoApp/1.0");
			HttpResponseMessage httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);
			if (httpResponse.IsSuccessStatusCode)
			{
				NominatimResponse nominatimResponse = await httpResponse.Content.ReadFromJsonAsync<NominatimResponse>(cancellationToken);
				string city = nominatimResponse?.Address?.City ?? nominatimResponse?.Address?.Town ?? nominatimResponse?.Address?.Village ?? nominatimResponse?.Address?.County;
				if (!string.IsNullOrEmpty(city))
				{
					return city;
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			this._logger.LogWarning(ex2, "Failed to resolve city name for coordinates {@Latitude}, {@Longitude}", latitude, longitude);
		}
		return "Unknown Location";
	}
}
