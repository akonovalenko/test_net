using MarketRateInfo.Server.BusinessLogic.Exceptions;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MarketRateInfo.Server.BusinessLogic.ExternalRates.Models;
using MarketRateInfo.Server.BusinessLogic.Models.Requests;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;

namespace MarketRateInfo.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RatesController(
    IMarketRateService marketRateService,
    IMarketRateRefreshService refreshService,
    ILogger<RatesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MarketRateResponse>>> GetRates(
        [FromQuery] string symbol,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var rates = await marketRateService.GetHistoryAsync(
            symbol, from, to, cancellationToken);

        return Ok(rates);
    }

    [HttpGet("symbols")]
    public async Task<ActionResult<IEnumerable<string>>> GetSymbols(
        CancellationToken cancellationToken)
    {
        return Ok(await marketRateService.GetSymbolsAsync(cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("symbols")]
    public async Task<IActionResult> AddSymbol(
        [FromBody] AddSymbolRequest request,
        CancellationToken cancellationToken)
    {
        await marketRateService.AddCurrencyPairAsync(request.Symbol, cancellationToken);
        var today = DateTime.UtcNow.Date;
        await refreshService.RefreshAsync(
            request.Symbol,
            today,
            today,
            cancellationToken);

        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("symbols")]
    public async Task<IActionResult> RemoveCurrencyPair(
        [FromQuery] string symbol,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("RemoveCurrencyPair called. symbol={Symbol}", symbol);

        if (string.IsNullOrWhiteSpace(symbol))
        {
            logger.LogWarning("RemoveCurrencyPair: missing symbol query parameter");
            return BadRequest(new { message = "The 'symbol' query parameter is required." });
        }

        if (!ModelState.IsValid)
        {
            logger.LogWarning("RemoveCurrencyPair: invalid model state: {ModelState}", ModelState);
            return BadRequest(new { message = "One or more validation errors occurred." });
        }

        try
        {
            await marketRateService.RemoveCurrencyPairAsync(symbol, cancellationToken);
            return Ok();
        }
        catch (BusinessValidationException ex)
        {
            logger.LogInformation("RemoveCurrencyPair validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoveCurrencyPair unexpected error for symbol={Symbol}", symbol);
            return StatusCode(500, new { message = "An unexpected error occurred." });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("refresh")]
    public async Task<ActionResult<MarketRateRefreshResult>> RefreshRates(
    [FromQuery] string? symbol,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    CancellationToken cancellationToken)
    {
        // Если даты не переданы, по умолчанию запрашиваем текущий день
        var from = startDate ?? DateTime.UtcNow.Date;
        var to = endDate ?? DateTime.UtcNow.Date;

        var result = string.IsNullOrWhiteSpace(symbol)
            ? await refreshService.RefreshAsync(from, to, cancellationToken)
            : await refreshService.RefreshAsync(symbol, from, to, cancellationToken);

        return Ok(result);
    }
}
