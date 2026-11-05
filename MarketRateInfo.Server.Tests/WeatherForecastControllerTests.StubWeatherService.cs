using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// the WeatherForecastControllerTests class contains unit tests for the WeatherForecastController, 
/// which is responsible for handling requests related to weather forecasts. 
/// The tests verify that the controller correctly returns service responses and forwards 
/// cancellation tokens to the underlying service layer.
/// </summary>
public sealed partial class WeatherForecastControllerTests
{
    /// <summary>
    /// the StubWeatherService class is a stub implementation of the IWeatherService interface,
    /// </summary>
    /// <param name="forecasts">the list of weather forecasts to return</param>
    private sealed class StubWeatherService(IEnumerable<WeatherForecast>? forecasts = null) : IWeatherService
    {
        public (double Latitude, double Longitude)? LastCoordinates { get; private set; }

        public Task<IEnumerable<WeatherForecast>> GetForecastAsync(
            double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            LastCoordinates = (latitude, longitude);
            return Task.FromResult(forecasts ?? []);
        }
    }
}
