using MarketRateInfo.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;

namespace MarketRateInfo.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class HomeController(IMarketRateService marketRateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HomeResponse>> Get(
        CancellationToken cancellationToken)
    {
        var response = await marketRateService.GetHomeAsync(cancellationToken);
        return Ok(response);
    }
}
