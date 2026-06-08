namespace RulePlugins.Core;

internal static class ConditionParameters
{
    public static string? Get(IReadOnlyDictionary<string, string> parameters, string key)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    public static HashSet<string> GetSet(IReadOnlyDictionary<string, string> parameters, string key)
    {
        var raw = Get(parameters, key);
        return raw is null
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
