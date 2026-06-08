namespace RulesService.SmartLinks.Configuration;

public class RulesOptions
{
    public const string SectionName = "Rules";

    public string RulesDirectory { get; set; } = "Rules";

    public string PluginDirectory { get; set; } = "Plugins";

    public Uri? BaseUrl { get; set; }

    public Uri? DefaultUrl { get; set; }

    public List<SmartLinkDefinition> Links { get; set; } = [];
}

public class SmartLinkDefinition
{
    public string Code { get; set; } = string.Empty;

    public Uri? FallbackUrl { get; set; }

    public List<RedirectRuleOptions> Rules { get; set; } = [];
}

public class RedirectRuleOptions
{
    public string Name { get; set; } = string.Empty;

    public Uri TargetUrl { get; set; } = new("about:blank");

    public List<ConditionOptions> Conditions { get; set; } = [];
}

public class ConditionOptions
{
    public string Type { get; set; } = string.Empty;

    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
