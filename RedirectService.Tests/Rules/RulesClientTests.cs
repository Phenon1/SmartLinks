using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using RedirectService.SmartLinks.Rules;
using SmartLinks.Contracts;

namespace RedirectService.Tests.Rules;

public class RulesClientTests
{
    [Fact]
    public async Task HandleAsyncSendsOriginalHttpContextSnapshot()
    {
        RuleHandlerRequest? captured = null;
        var client = CreateClient(new StubHandler(async request =>
        {
            captured = await request.Content!.ReadFromJsonAsync<RuleHandlerRequest>();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""{"found":true,"targetUrl":"https://example.com/","ruleName":"desktop","isFallback":false}""")
            };
        }));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/s/promo";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.QueryString = new QueryString("?country=RU");
        httpContext.Request.Headers.UserAgent = "Mozilla/5.0 Chrome/120.0";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var response = await client.HandleAsync(httpContext, "promo");

        Assert.True(response.Found);
        Assert.Equal("desktop", response.RuleName);
        Assert.NotNull(captured);
        Assert.Equal("promo", captured.Code);
        Assert.Equal("RU", captured.Query["country"]);
        Assert.Equal("Mozilla/5.0 Chrome/120.0", captured.Headers["User-Agent"]);
    }

    [Fact]
    public async Task HandleAsyncReturnsNotFoundWhenRulesServiceFails()
    {
        var client = CreateClient(new ThrowingHandler());
        var httpContext = new DefaultHttpContext();

        var response = await client.HandleAsync(httpContext, "promo");

        Assert.False(response.Found);
    }

    private static RulesClient CreateClient(HttpMessageHandler handler)
    {
        return new RulesClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://rules.test") },
            NullLogger<RulesClient>.Instance);
    }

    private static StringContent JsonContent(string json)
    {
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/rules/handle", request.RequestUri?.PathAndQuery);
            return _handler(request);
        }
    }

    private class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("сервис недоступен");
        }
    }
}
