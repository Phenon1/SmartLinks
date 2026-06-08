using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RulesService.SmartLinks;
using RulesService.SmartLinks.Configuration;

namespace RedirectService.Tests.RulesService;

public class RulesCatalogTests
{
    [Fact]
    public void LoadReadsRulesFromRulesDirectory()
    {
        var directory = CreateTempRulesDirectory();
        File.WriteAllText(Path.Combine(directory, "Rules", "promo.json"), """
        {
          "Links": [
            {
              "Code": "promo",
              "Rules": [
                {
                  "Name": "desktop",
                  "TargetUrl": "https://example.com",
                  "Conditions": []
                }
              ]
            }
          ]
        }
        """);
        var catalog = CreateCatalog(directory);

        var result = catalog.Load();

        var promo = Assert.Single(result.Links);
        Assert.Equal("promo", promo.Code);
        Assert.Equal("desktop", Assert.Single(promo.Rules).Name);
    }

    [Fact]
    public void LoadKeepsRulesInMemoryUntilRulesDirectoryChanges()
    {
        var directory = CreateTempRulesDirectory();
        var rulesPath = Path.Combine(directory, "Rules", "promo.json");
        File.WriteAllText(rulesPath, RuleFile("first"));
        var catalog = CreateCatalog(directory);

        Assert.Equal("first", Assert.Single(Assert.Single(catalog.Load().Links).Rules).Name);

        File.WriteAllText(rulesPath, RuleFile("second"));

        var updated = WaitForUpdatedRule(catalog, "second");
        Assert.Equal("second", updated);
    }

    [Fact]
    public void LoadFindsNewRuleFileAfterCatalogWasCached()
    {
        var directory = CreateTempRulesDirectory();
        File.WriteAllText(Path.Combine(directory, "Rules", "promo.json"), RuleFile("first"));
        var catalog = CreateCatalog(directory);

        Assert.Equal("promo", Assert.Single(catalog.Load().Links).Code);

        File.WriteAllText(Path.Combine(directory, "Rules", "promo_new.json"), RuleFile("second", "promo_new"));

        var links = catalog.Load().Links;

        Assert.Contains(links, link => link.Code == "promo");
        Assert.Contains(links, link => link.Code == "promo_new");
    }

    [Fact]
    public void LoadIgnoresInvalidJsonRuleFile()
    {
        var directory = CreateTempRulesDirectory();
        File.WriteAllText(Path.Combine(directory, "Rules", "broken.json"), "{");
        var catalog = CreateCatalog(directory);

        Assert.Empty(catalog.Load().Links);
    }

    [Fact]
    public void LoadUsesFallbackFromJsonRuleFile()
    {
        var directory = CreateTempRulesDirectory();
        File.WriteAllText(Path.Combine(directory, "Rules", "default.json"), """
        {
          "DefaultUrl": "https://rules-default.example.com",
          "Links": [
            {
              "Code": "new",
              "FallbackUrl": "https://new.example.com"
            }
          ]
        }
        """);
        var catalog = CreateCatalog(directory);

        var result = catalog.Load();

        Assert.Equal("https://rules-default.example.com/", result.DefaultUrl!.ToString());
        Assert.Equal("new", Assert.Single(result.Links).Code);
    }

    private static RulesCatalog CreateCatalog(string directory)
    {
        return new RulesCatalog(
            new TestOptionsMonitor<RulesOptions>(new RulesOptions { RulesDirectory = "Rules" }),
            new TestHostEnvironment(directory),
            NullLogger<RulesCatalog>.Instance);
    }

    private static string CreateTempRulesDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "Rules"));
        return directory;
    }

    private static string RuleFile(string ruleName, string code = "promo")
    {
        return $$"""
        {
          "Links": [
            {
              "Code": "{{code}}",
              "Rules": [
                {
                  "Name": "{{ruleName}}",
                  "TargetUrl": "https://example.com",
                  "Conditions": []
                }
              ]
            }
          ]
        }
        """;
    }

    private static string WaitForUpdatedRule(RulesCatalog catalog, string expected)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(3);
        string current;

        do
        {
            current = Assert.Single(Assert.Single(catalog.Load().Links).Rules).Name;
            if (current == expected)
            {
                return current;
            }

            Thread.Sleep(50);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return current;
    }

    private class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "RedirectService.Tests";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
