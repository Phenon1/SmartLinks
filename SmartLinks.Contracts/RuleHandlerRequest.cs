namespace SmartLinks.Contracts;

public record RuleHandlerRequest(
    string Code,
    DateTimeOffset RequestedAt,
    string Method,
    string Path,
    string? IpAddress,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers);
