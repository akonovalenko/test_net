using MarketRateInfo.Server.BusinessLogic.Models.Responses;
using MarketRateInfo.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MarketRateInfo.Server.Tests;

/// <summary>
/// the HomeControllerTests class contains unit tests for the HomeController, which is responsible for handling 
/// requests related to the home endpoint of the Market Rate Info application. 
/// The tests verify that the controller correctly returns service responses and forwards cancellation tokens 
/// to the underlying service layer.
/// </summary>
public sealed partial class HomeControllerTests
{
    [Fact]
    public async Task Get_ReturnsServiceResponse()
    {
        var expected = new HomeResponse(
            "Market Rate Info",
            "ok",
            "SQLite",
            [new HomeRateResponse("EUR/USD", 1.17m, DateTime.UtcNow)]);

        var service = new StubMarketRateService { HomeResponse = expected };
        var controller = new HomeController(service);

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal(1, service.GetHomeCalls);
    }

    [Fact]
    public async Task Get_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var service = new StubMarketRateService();
        var controller = new HomeController(service);

        await controller.Get(cts.Token);

        Assert.Equal(cts.Token, service.LastCancellationToken);
    }
}
