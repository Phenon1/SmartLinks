using System.Text.Json;
using Microsoft.Extensions.Options;
using RulesService.SmartLinks.Configuration;
using RulesService.SmartLinks.Interfaces;

namespace RulesService.SmartLinks;

public class RulesCatalog : IRulesCatalog, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHostEnvironment _environment;
    private readonly ILogger<RulesCatalog> _logger;
    private readonly IOptionsMonitor<RulesOptions> _options;
    private readonly object _syncRoot = new();
    private FileSystemWatcher? _watcher;
    private RulesOptions? _cachedRules;
    private string? _cachedRulesSignature;
    private string? _watchedDirectory;
    private bool _disposed;

    public RulesCatalog(
        IOptionsMonitor<RulesOptions> options,
        IHostEnvironment environment,
        ILogger<RulesCatalog> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    public RulesOptions Load()
    {
        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var currentOptions = _options.CurrentValue;
            var rulesDirectory = ResolvePath(currentOptions.RulesDirectory);
            EnsureWatcher(rulesDirectory);
            var rulesSignature = CreateRulesSignature(rulesDirectory);

            if (_cachedRules is null || _cachedRulesSignature != rulesSignature)
            {
                _cachedRules = LoadFromDisk(currentOptions, rulesDirectory);
                _cachedRulesSignature = rulesSignature;
            }

            return Clone(_cachedRules);
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _disposed = true;
            _watcher?.Dispose();
            _watcher = null;
            _cachedRules = null;
            _cachedRulesSignature = null;
        }
    }

    private RulesOptions LoadFromDisk(RulesOptions currentOptions, string rulesDirectory)
    {
        var result = Clone(currentOptions);
        result.Links.Clear();

        foreach (var rulesFile in LoadRuleFiles(rulesDirectory))
        {
            if (rulesFile.BaseUrl is not null)
            {
                result.BaseUrl = rulesFile.BaseUrl;
            }

            if (rulesFile.DefaultUrl is not null)
            {
                result.DefaultUrl = rulesFile.DefaultUrl;
            }

            MergeLinks(result.Links, rulesFile.Links);
        }

        return result;
    }

    private IEnumerable<RulesOptions> LoadRuleFiles(string rulesDirectory)
    {
        if (!Directory.Exists(rulesDirectory))
        {
            return [];
        }

        var rules = new List<RulesOptions>();
        foreach (var file in Directory.EnumerateFiles(rulesDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var rulesFile = JsonSerializer.Deserialize<RulesOptions>(json, JsonOptions);
                if (rulesFile is not null)
                {
                    rules.Add(rulesFile);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Не удалось прочитать JSON-файл правил {RulesPath}", file);
            }
        }

        return rules;
    }

    private static RulesOptions Clone(RulesOptions source)
    {
        return new RulesOptions
        {
            RulesDirectory = source.RulesDirectory,
            PluginDirectory = source.PluginDirectory,
            BaseUrl = source.BaseUrl,
            DefaultUrl = source.DefaultUrl,
            Links = source.Links
                .Select(link => new SmartLinkDefinition
                {
                    Code = link.Code,
                    FallbackUrl = link.FallbackUrl,
                    Rules = link.Rules
                        .Select(rule => new RedirectRuleOptions
                        {
                            Name = rule.Name,
                            TargetUrl = rule.TargetUrl,
                            Conditions = rule.Conditions
                                .Select(condition => new ConditionOptions
                                {
                                    Type = condition.Type,
                                    Parameters = new Dictionary<string, string>(
                                        condition.Parameters,
                                        StringComparer.OrdinalIgnoreCase)
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static void MergeLinks(List<SmartLinkDefinition> target, IEnumerable<SmartLinkDefinition> source)
    {
        foreach (var sourceLink in source)
        {
            var existing = target.FirstOrDefault(item =>
                string.Equals(item.Code, sourceLink.Code, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                target.Add(sourceLink);
                continue;
            }

            if (sourceLink.FallbackUrl is not null)
            {
                existing.FallbackUrl = sourceLink.FallbackUrl;
            }

            existing.Rules.AddRange(sourceLink.Rules);
        }
    }

    private void EnsureWatcher(string rulesDirectory)
    {
        if (string.Equals(_watchedDirectory, rulesDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _watcher?.Dispose();
        _watcher = null;
        _watchedDirectory = rulesDirectory;
        _cachedRules = null;
        _cachedRulesSignature = null;

        Directory.CreateDirectory(rulesDirectory);

        _watcher = new FileSystemWatcher(rulesDirectory, "*.json")
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
            _cachedRules = null;
            _cachedRulesSignature = null;
        }
    }

    private string ResolvePath(string directory)
    {
        return Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(_environment.ContentRootPath, directory);
    }

    private static string CreateRulesSignature(string rulesDirectory)
    {
        if (!Directory.Exists(rulesDirectory))
        {
            return string.Empty;
        }

        return string.Join(
            '|',
            Directory.EnumerateFiles(rulesDirectory, "*.json")
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(file =>
                {
                    var info = new FileInfo(file);
                    return string.Join(
                        ':',
                        Path.GetFileName(file),
                        info.Length.ToString(),
                        info.LastWriteTimeUtc.Ticks.ToString());
                }));
    }
}
