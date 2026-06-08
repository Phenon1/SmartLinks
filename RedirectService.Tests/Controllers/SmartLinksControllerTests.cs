using Microsoft.AspNetCore.Http;
using RedirectService.Controllers;
using RedirectService.SmartLinks.Interfaces;
using SmartLinks.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace RedirectService.Tests.Controllers;

public class SmartLinksControllerTests
{
    [Fact]
    public async Task RedirectByCodeReturnsRedirectFromRulesService()
    {
        var controller = CreateController(RuleHandlerResponse.Rule(new Uri("https://example.com"), "desktop"));

        var result = await controller.RedirectByCode("promo");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com/", redirect.Url);
        Assert.Equal("desktop", controller.Response.Headers["X-SmartLink-Rule"]);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task RedirectByCodeReturnsNotFoundWhenRulesServiceDoesNotResolveLink()
    {
        var controller = CreateController(RuleHandlerResponse.NotFound());

        var result = await controller.RedirectByCode("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ResolveByCodeReturnsDecisionWithoutRedirect()
    {
        var controller = CreateController(RuleHandlerResponse.Rule(new Uri("https://example.com"), "desktop"));

        var result = await controller.ResolveByCode("promo");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RuleHandlerResponse>(ok.Value);
        Assert.True(response.Found);
        Assert.Equal("desktop", response.RuleName);
        Assert.Equal("https://example.com/", response.TargetUrl!.ToString());
    }

    private static SmartLinksController CreateController(RuleHandlerResponse response)
    {
        return new SmartLinksController(new StubRulesClient(response))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private class StubRulesClient : IRulesClient
    {
        private readonly RuleHandlerResponse _response;

        public StubRulesClient(RuleHandlerResponse response)
        {
            _response = response;
        }

        public Task<RuleHandlerResponse> HandleAsync(HttpContext httpContext, string code)
        {
            return Task.FromResult(_response);
        }
    }
}
