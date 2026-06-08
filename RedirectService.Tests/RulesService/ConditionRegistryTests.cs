using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SmartLinks.Contracts;
using RulesService.SmartLinks.Conditions;
using RulesService.SmartLinks.Configuration;
using RulesService.SmartLinks.Interfaces;

namespace RedirectService.Tests.RulesService;

public class ConditionRegistryTests
{
    [Fact]
    public void TryGetReloadsPluginConditionsAfterPluginDirectoryChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Plugins"));
        var loader = new TestPluginLoader();
        using var registry = new ConditionRegistry(
            loader,
            new TestHostEnvironment(root),
            new TestOptionsMonitor<RulesOptions>(new RulesOptions { PluginDirectory = "Plugins" }));

        Assert.True(registry.TryGet("browser", out _));
        Assert.False(registry.TryGet("country", out _));
        Assert.Equal(1, loader.LoadCount);

        File.WriteAllText(Path.Combine(root, "Plugins", "updated.dll"), string.Empty);

        Assert.True(WaitForCondition(registry, "country"));
        Assert.True(loader.LoadCount >= 2);
    }

    private static bool WaitForCondition(ConditionRegistry registry, string type)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(3);
        do
        {
            if (registry.TryGet(type, out _))
            {
                return true;
            }

            Thread.Sleep(50);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return false;
    }

    private class TestPluginLoader : IConditionPluginLoader
    {
        public int LoadCount { get; private set; }

        public IEnumerable<IRuleCondition> Load(string pluginDirectory)
        {
            LoadCount++;

            return LoadCount == 1
                ? [new TestCondition("browser")]
                : [new TestCondition("country")];
        }
    }

    private class TestCondition(string type) : IRuleCondition
    {
        public string Type { get; } = type;

        public bool IsMatch(RuleHttpContext context, IReadOnlyDictionary<string, string> parameters)
        {
            return true;
        }
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
