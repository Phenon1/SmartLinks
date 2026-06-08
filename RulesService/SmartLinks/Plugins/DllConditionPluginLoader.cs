using System.Reflection;
using System.Runtime.Loader;
using RulesService.SmartLinks.Interfaces;
using SmartLinks.Contracts;

namespace RulesService.SmartLinks.Plugins;

public class DllConditionPluginLoader : IConditionPluginLoader
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DllConditionPluginLoader> _logger;

    public DllConditionPluginLoader(IHostEnvironment environment, ILogger<DllConditionPluginLoader> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public IEnumerable<IRuleCondition> Load(string pluginDirectory)
    {
        var fullPath = Path.IsPathRooted(pluginDirectory)
            ? pluginDirectory
            : Path.Combine(_environment.ContentRootPath, pluginDirectory);

        if (!Directory.Exists(fullPath))
        {
            return [];
        }

        var plugins = new List<IRuleCondition>();
        foreach (var file in Directory.EnumerateFiles(fullPath, "*.dll"))
        {
            TryLoad(file, plugins);
        }

        return plugins;
    }

    private void TryLoad(string path, ICollection<IRuleCondition> plugins)
    {
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));
            var pluginTypes = assembly.GetTypes()
                .Where(type => typeof(IRuleCondition).IsAssignableFrom(type)
                    && type is { IsAbstract: false, IsInterface: false }
                    && type.GetConstructor(Type.EmptyTypes) is not null);

            foreach (var pluginType in pluginTypes)
            {
                if (Activator.CreateInstance(pluginType) is IRuleCondition plugin)
                {
                    plugins.Add(plugin);
                    _logger.LogInformation(
                        "Загружен плагин условия правил {ConditionType} из {PluginPath}",
                        plugin.Type,
                        path);
                }
            }
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or ReflectionTypeLoadException)
        {
            _logger.LogWarning(ex, "Не удалось загрузить DLL-плагин правил {PluginPath}", path);
        }
    }
}
