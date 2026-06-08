using RulePlugins.Core;
using RulesService.SmartLinks;
using RulesService.SmartLinks.Conditions;
using RulesService.SmartLinks.Configuration;
using RulesService.SmartLinks.Context;
using RulesService.SmartLinks.Interfaces;
using SmartLinks.Contracts;

namespace RedirectService.Tests.RulesService;

public class RuleHandlerTests
{
    [Fact]
    public void HandleReturnsFirstMatchingRuleFromBaseRules()
    {
        var handler = CreateHandler(new RulesOptions
        {
            Links =
            [
                new SmartLinkDefinition
                {
                    Code = "promo",
                    FallbackUrl = new Uri("https://example.com/fallback"),
                    Rules =
                    [
                        Rule("desktop", "https://example.com/desktop", "device", "is", "desktop")
                    ]
                }
            ]
        });

        var result = handler.Handle(Request(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 Chrome/120.0"
        }));

        Assert.True(result.Found);
        Assert.False(result.IsFallback);
        Assert.Equal("desktop", result.RuleName);
        Assert.Equal("https://example.com/desktop", result.TargetUrl!.ToString());
    }

    [Fact]
    public void HandleReturnsFallbackWhenNoRuleMatches()
    {
        var handler = CreateHandler(new RulesOptions
        {
            Links =
            [
                new SmartLinkDefinition
                {
                    Code = "promo",
                    FallbackUrl = new Uri("https://example.com/fallback"),
                    Rules =
                    [
                        Rule("mobile", "https://example.com/mobile", "device", "is", "mobile")
                    ]
                }
            ]
        });

        var result = handler.Handle(Request());

        Assert.True(result.IsFallback);
        Assert.Equal("https://example.com/fallback", result.TargetUrl!.ToString());
    }

    [Fact]
    public void HandleUsesGlobalFallbackForUnknownCode()
    {
        var handler = CreateHandler(new RulesOptions
        {
            DefaultUrl = new Uri("https://example.com")
        });

        var result = handler.Handle(Request(code: "missing"));

        Assert.True(result.IsFallback);
        Assert.Equal("https://example.com/", result.TargetUrl!.ToString());
    }

    [Fact]
    public void HandleResolvesRelativeRuleUrlFromConfiguredBaseUrl()
    {
        var handler = CreateHandler(new RulesOptions
        {
            BaseUrl = new Uri("https://example.com"),
            Links =
            [
                new SmartLinkDefinition
                {
                    Code = "promo",
                    Rules =
                    [
                        Rule("desktop", "/desktop", "device", "is", "desktop")
                    ]
                }
            ]
        });

        var result = handler.Handle(Request(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 Chrome/120.0"
        }));

        Assert.True(result.Found);
        Assert.Equal("https://example.com/desktop", result.TargetUrl!.ToString());
    }

    [Fact]
    public void HandleReturnsNotFoundForRelativeRuleUrlWithoutBaseUrl()
    {
        var handler = CreateHandler(new RulesOptions
        {
            Links =
            [
                new SmartLinkDefinition
                {
                    Code = "promo",
                    Rules =
                    [
                        Rule("desktop", "/desktop", "device", "is", "desktop")
                    ]
                }
            ]
        });

        var result = handler.Handle(Request(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 Chrome/120.0"
        }));

        Assert.False(result.Found);
    }

    [Fact]
    public void HandleReturnsNotFoundWithoutAnyFallback()
    {
        var result = CreateHandler(new RulesOptions()).Handle(Request(code: "missing"));

        Assert.False(result.Found);
    }

    [Fact]
    public void HandleCanUseConditionLoadedFromDllPlugin()
    {
        var handler = new RuleHandler(
            new ConditionRegistry([new BrowserCondition()]),
            new StubCatalog(new RulesOptions
            {
                Links =
                [
                    new SmartLinkDefinition
                    {
                        Code = "promo",
                        Rules =
                        [
                            Rule("firefox", "https://example.com/firefox", "browser", "is", "firefox")
                        ]
                    }
                ]
            }),
            new DefaultRuleHttpContextFactory());

        var result = handler.Handle(Request(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 Firefox/125.0"
        }));

        Assert.True(result.Found);
        Assert.Equal("firefox", result.RuleName);
        Assert.Equal("https://example.com/firefox", result.TargetUrl!.ToString());
    }

    [Fact]
    public void HandleUsesInjectedRuleHttpContextFactory()
    {
        var handler = new RuleHandler(
            new ConditionRegistry([new DeviceCondition()]),
            new StubCatalog(new RulesOptions
            {
                Links =
                [
                    new SmartLinkDefinition
                    {
                        Code = "promo",
                        Rules =
                        [
                            Rule("forced-mobile", "https://example.com/mobile", "device", "is", "mobile")
                        ]
                    }
                ]
            }),
            new FixedDeviceContextFactory("mobile"));

        var result = handler.Handle(Request(headers: new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 Chrome/120.0"
        }));

        Assert.True(result.Found);
        Assert.Equal("forced-mobile", result.RuleName);
    }

    private static RuleHandler CreateHandler(RulesOptions options)
    {
        return new RuleHandler(
            new ConditionRegistry([new DeviceCondition()]),
            new StubCatalog(options),
            new DefaultRuleHttpContextFactory());
    }

    private static RedirectRuleOptions Rule(string name, string targetUrl, string conditionType, string parameter, string value)
    {
        return new RedirectRuleOptions
        {
            Name = name,
            TargetUrl = new Uri(targetUrl, UriKind.RelativeOrAbsolute),
            Conditions =
            [
                new ConditionOptions
                {
                    Type = conditionType,
                    Parameters = new Dictionary<string, string> { [parameter] = value }
                }
            ]
        };
    }

    private static RuleHandlerRequest Request(
        string code = "promo",
        IReadOnlyDictionary<string, string>? headers = null)
    {
        return new RuleHandlerRequest(
            code,
            new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero),
            "GET",
            "/s/promo",
            "127.0.0.1",
            new Dictionary<string, string>(),
            headers ?? new Dictionary<string, string>());
    }

    private class StubCatalog : IRulesCatalog
    {
        private readonly RulesOptions _options;

        public StubCatalog(RulesOptions options)
        {
            _options = options;
        }

        public RulesOptions Load() => _options;
    }

    private class FixedDeviceContextFactory : IRuleHttpContextFactory
    {
        private readonly string _device;

        public FixedDeviceContextFactory(string device)
        {
            _device = device;
        }

        public RuleHttpContext Create(RuleHandlerRequest request)
        {
            return new RuleHttpContext(request, null, null, _device, null);
        }
    }
}
