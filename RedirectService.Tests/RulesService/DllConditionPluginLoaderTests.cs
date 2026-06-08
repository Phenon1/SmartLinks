using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RulesService.SmartLinks.Plugins;

namespace RedirectService.Tests.RulesService;

public class DllConditionPluginLoaderTests
{
    [Fact]
    public void LoadReturnsConditionTypesFromDllPlugins()
    {
        var root = FindSolutionRoot();
        var loader = new DllConditionPluginLoader(
            new TestHostEnvironment(Path.Combine(root, "RulesService")),
            NullLogger<DllConditionPluginLoader>.Instance);

        var conditions = loader.Load(Path.Combine(root, "RulesService", "Plugins")).ToArray();

        var conditionTypes = conditions.Select(condition => condition.Type).Order().ToArray();

        Assert.Equal(new[] { "browser", "country", "device", "time" }, conditionTypes);
    }

    [Fact]
    public void LoadReturnsEmptySetWhenDirectoryDoesNotExist()
    {
        var loader = new DllConditionPluginLoader(
            new TestHostEnvironment(AppContext.BaseDirectory),
            NullLogger<DllConditionPluginLoader>.Instance);

        var conditions = loader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.Empty(conditions);
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

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SmartLinks.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Не удалось найти SmartLinks.slnx");
    }
}
