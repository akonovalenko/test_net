using MarketRateInfo.Server.BusinessLogic.Models;

namespace MarketRateInfo.Server.BusinessLogic.Interfaces;

/// <summary>
/// Provides weather forecast retrieval services.
/// </summary>
public interface IWeatherService
{
	/// <summary>
	/// Retrieves a collection of weather forecasts for the specified geographic coordinates.
	/// </summary>
	/// <param name="latitude">Latitude of the location.</param>
	/// <param name="longitude">Longitude of the location.</param>
	/// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a collection of weather forecasts.</returns>
	Task<IEnumerable<WeatherForecast>> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default(CancellationToken));
}
