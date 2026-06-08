namespace SmartLinks.Contracts;

public record RuleHandlerResponse(
    bool Found,
    Uri? TargetUrl,
    string? RuleName,
    bool IsFallback)
{
    public static RuleHandlerResponse NotFound() => new(false, null, null, false);

    public static RuleHandlerResponse Rule(Uri targetUrl, string ruleName) => new(true, targetUrl, ruleName, false);

    public static RuleHandlerResponse Fallback(Uri targetUrl) => new(true, targetUrl, null, true);
}
