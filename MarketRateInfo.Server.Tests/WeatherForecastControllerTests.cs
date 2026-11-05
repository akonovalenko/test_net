using MarketRateInfo.Server.BusinessLogic.Models;
using MarketRateInfo.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// The WeatherForecastControllerTests class contains unit tests for the WeatherForecastController,
/// which is responsible for handling requests related to weather forecasts. 
/// The tests verify that the controller correctly handles missing parameters, 
/// returns forecasts for valid coordinates, and forwards cancellation tokens to the underlying service layer.
/// </summary>
public sealed partial class WeatherForecastControllerTests
{
    [Fact]
    public async Task Get_ReturnsBadRequestWhenLatitudeIsMissing()
    {
        var controller = new WeatherForecastController(new StubWeatherService());

        var result = await controller.Get(null, 10, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsBadRequestWhenLongitudeIsMissing()
    {
        var controller = new WeatherForecastController(new StubWeatherService());

        var result = await controller.Get(10, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsForecastForCoordinates()
    {
        var expected = new[]
        {
            new WeatherForecast
            {
                Date = new DateOnly(2026, 1, 5),
                TemperatureC = 18,
                Summary = "Mild",
                CityName = "Dnipro"
            }
        };
        var service = new StubWeatherService(expected);
        var controller = new WeatherForecastController(service);

        var result = await controller.Get(48.45, 35.05, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var forecasts = Assert.IsType<WeatherForecast[]>(ok.Value);
        Assert.Single(forecasts);
        Assert.Equal("Dnipro", forecasts[0].CityName);
        Assert.Equal((48.45, 35.05), service.LastCoordinates!.Value);
    }
}
