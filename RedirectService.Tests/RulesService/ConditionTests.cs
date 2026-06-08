using RulePlugins.Core;
using SmartLinks.Contracts;

namespace RedirectService.Tests.RulesService;

public class ConditionTests
{
    private static RuleHttpContext Context(
        DateTimeOffset? now = null,
        IReadOnlyDictionary<string, string>? query = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string? ip = "127.0.0.1")
    {
        return new RuleHttpContext(new RuleHandlerRequest(
            "promo",
            now ?? new DateTimeOffset(2026, 5, 30, 4, 30, 0, TimeSpan.Zero),
            "GET",
            "/s/promo",
            ip,
            query ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
    }

    [Fact]
    public void CountryConditionUsesQueryHeaderOrIpCountry()
    {
        var condition = new CountryCondition();

        Assert.True(condition.IsMatch(Context(query: new Dictionary<string, string> { ["country"] = "ru" }), Parameters("is", "RU")));
        Assert.True(condition.IsMatch(Context(headers: new Dictionary<string, string> { ["X-Country"] = "US" }), Parameters("is", "US")));
        Assert.True(condition.IsMatch(Context(ip: "10.1.2.3"), Parameters("is", "RU")));
    }

    [Fact]
    public void DeviceAndBrowserConditionsUseSameRuleHttpContext()
    {
        var context = Context(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (iPad) AppleWebKit Chrome/120.0 Edg/120.0"
        });

        Assert.True(new DeviceCondition().IsMatch(context, Parameters("is", "tablet")));
        Assert.True(new BrowserCondition().IsMatch(context, Parameters("is", "edge")));
    }

    [Theory]
    [InlineData("Mozilla/5.0 Firefox/125.0", "firefox")]
    [InlineData("Mozilla/5.0 CriOS/125.0", "chrome")]
    [InlineData("Mozilla/5.0 Safari/604.1", "safari")]
    [InlineData("", null)]
    public void BrowserDetectionCoversSupportedUserAgents(string userAgent, string? expected)
    {
        var context = Context(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = userAgent
        });

        Assert.Equal(expected, context.Browser);
    }

    [Fact]
    public void TimeWindowConditionSupportsOffsetAndOvernightWindows()
    {
        var condition = new TimeWindowCondition();

        Assert.True(condition.IsMatch(Context(), new Dictionary<string, string>
        {
            ["from"] = "06:00",
            ["to"] = "12:00",
            ["utcOffset"] = "03:00"
        }));
        Assert.True(condition.IsMatch(Context(now: new DateTimeOffset(2026, 5, 30, 23, 0, 0, TimeSpan.Zero)), new Dictionary<string, string>
        {
            ["from"] = "22:00",
            ["to"] = "03:00"
        }));
        Assert.False(condition.IsMatch(Context(), Parameters("from", "bad")));
    }

    [Fact]
    public void RuleHttpContextExposesCodeAndUnknownCountryWhenIpMissing()
    {
        var context = Context(ip: null);

        Assert.Equal("promo", context.Code);
        Assert.Null(context.Country);
    }

    private static Dictionary<string, string> Parameters(string key, string value)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [key] = value
        };
    }
}
