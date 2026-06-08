using RulesService.SmartLinks.Context;
using SmartLinks.Contracts;

namespace RedirectService.Tests.RulesService;

public class RuleHttpContextFactoryTests
{
    [Fact]
    public void CreateUsesFirstForwardedForAddress()
    {
        var context = new DefaultRuleHttpContextFactory().Create(Request(
            "::ffff:172.18.0.1",
            new Dictionary<string, string>
            {
                ["X-Forwarded-For"] = "203.0.113.10, 172.18.0.1"
            }));

        Assert.Equal("203.0.113.10", context.IpAddress);
    }

    [Fact]
    public void CreateNormalizesDockerMappedIpv4Address()
    {
        var context = new DefaultRuleHttpContextFactory().Create(Request("::ffff:172.18.0.1"));

        Assert.Equal("172.18.0.1", context.IpAddress);
    }

    [Fact]
    public void CreateReadsForwardedHeaderForAddress()
    {
        var context = new DefaultRuleHttpContextFactory().Create(Request(
            "172.18.0.1",
            new Dictionary<string, string>
            {
                ["Forwarded"] = "for=\"198.51.100.20:1234\";proto=https"
            }));

        Assert.Equal("198.51.100.20", context.IpAddress);
    }

    private static RuleHandlerRequest Request(
        string? ipAddress,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        return new RuleHandlerRequest(
            "promo",
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
            "GET",
            "/s/promo",
            ipAddress,
            new Dictionary<string, string>(),
            headers ?? new Dictionary<string, string>());
    }
}
