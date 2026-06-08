using Microsoft.Extensions.Options;
using RulesService.SmartLinks.Configuration;
using RulesService.SmartLinks.Interfaces;

namespace RulesService.SmartLinks.Conditions;

public class ConditionRegistry : IDisposable
{
    private readonly IConditionPluginLoader? _pluginLoader;
    private readonly IHostEnvironment? _environment;
    private readonly IOptionsMonitor<RulesOptions>? _options;
    private readonly IReadOnlyDictionary<string, IRuleCondition>? _fixedConditions;
    private readonly object _syncRoot = new();
    private FileSystemWatcher? _watcher;
    private IReadOnlyDictionary<string, IRuleCondition>? _cachedConditions;
    private string? _watchedDirectory;
    private bool _disposed;

    public ConditionRegistry(IEnumerable<IRuleCondition> conditions)
    {
        _fixedConditions = CreateRegistry(conditions);
    }

    public ConditionRegistry(
        IConditionPluginLoader pluginLoader,
        IHostEnvironment environment,
        IOptionsMonitor<RulesOptions> options)
    {
        _pluginLoader = pluginLoader;
        _environment = environment;
        _options = options;
    }

    public bool TryGet(string type, out IRuleCondition condition)
    {
        return GetConditions().TryGetValue(type, out condition!);
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _disposed = true;
            _watcher?.Dispose();
            _watcher = null;
            _cachedConditions = null;
        }
    }

    private IReadOnlyDictionary<string, IRuleCondition> GetConditions()
    {
        if (_fixedConditions is not null)
        {
            return _fixedConditions;
        }

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var options = _options?.CurrentValue ?? new RulesOptions();
            var pluginDirectory = ResolvePath(options.PluginDirectory);
            EnsureWatcher(pluginDirectory);

            _cachedConditions ??= CreateRegistry(_pluginLoader?.Load(options.PluginDirectory) ?? []);

            return _cachedConditions;
        }
    }

    private static IReadOnlyDictionary<string, IRuleCondition> CreateRegistry(IEnumerable<IRuleCondition> conditions)
    {
        var registry = new Dictionary<string, IRuleCondition>(StringComparer.OrdinalIgnoreCase);
        foreach (var condition in conditions)
        {
            registry[condition.Type] = condition;
        }

        return registry;
    }

    private void EnsureWatcher(string pluginDirectory)
    {
        if (string.Equals(_watchedDirectory, pluginDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _watcher?.Dispose();
        _watcher = null;
        _watchedDirectory = pluginDirectory;
        _cachedConditions = null;

        Directory.CreateDirectory(pluginDirectory);

        _watcher = new FileSystemWatcher(pluginDirectory, "*.dll")
        {
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
                | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Changed += (_, _) => InvalidateCache();
        _watcher.Created += (_, _) => InvalidateCache();
        _watcher.Deleted += (_, _) => InvalidateCache();
        _watcher.Renamed += (_, _) => InvalidateCache();
    }

    private void InvalidateCache()
    {
        lock (_syncRoot)
        {
            _cachedConditions = null;
        }
    }

    private string ResolvePath(string directory)
    {
        return Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(_environment?.ContentRootPath ?? AppContext.BaseDirectory, directory);
    }
}
