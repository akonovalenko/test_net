using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MarketRateInfo.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class WeatherForecastController(IWeatherService weatherService) : ControllerBase
    {
        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<ActionResult<IEnumerable<WeatherForecast>>> Get(
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            CancellationToken cancellationToken = default)
        {
            if (!latitude.HasValue || !longitude.HasValue)
            {
                return BadRequest("Latitude and longitude parameters are required.");
            }

            var data = await weatherService.GetForecastAsync(latitude.Value, longitude.Value, cancellationToken);
            return Ok(data.ToArray());
        }
    }
}