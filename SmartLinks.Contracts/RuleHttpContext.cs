namespace SmartLinks.Contracts;

public class RuleHttpContext
{
    public RuleHttpContext(RuleHandlerRequest request)
        : this(request, null, null, null, null)
    {
    }

    public RuleHttpContext(
        RuleHandlerRequest request,
        string? ipAddress,
        string? country,
        string? device,
        string? browser)
    {
        Request = request;
        Headers = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase);
        Query = new Dictionary<string, string>(request.Query, StringComparer.OrdinalIgnoreCase);
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? request.IpAddress : ipAddress;
        _country = NormalizeCountry(country);
        _device = string.IsNullOrWhiteSpace(device) ? null : device.Trim().ToLowerInvariant();
        _browser = string.IsNullOrWhiteSpace(browser) ? null : browser.Trim().ToLowerInvariant();
    }

    private readonly string? _country;
    private readonly string? _device;
    private readonly string? _browser;

    public RuleHandlerRequest Request { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public IReadOnlyDictionary<string, string> Query { get; }

    public string Code => Request.Code;

    public DateTimeOffset RequestedAt => Request.RequestedAt;

    public string? IpAddress { get; }

    public string? Country => _country ?? GetCountry();

    public string Device => _device ?? DetectDevice(UserAgent);

    public string? Browser => _browser ?? DetectBrowser(UserAgent);

    public string UserAgent => Headers.TryGetValue("User-Agent", out var value) ? value : string.Empty;

    private string? GetCountry()
    {
        if (Query.TryGetValue("country", out var queryCountry))
        {
            return NormalizeCountry(queryCountry);
        }

        if (Headers.TryGetValue("X-Country", out var explicitCountry))
        {
            return NormalizeCountry(explicitCountry);
        }

        return Headers.TryGetValue("CF-IPCountry", out var cloudflareCountry)
            ? NormalizeCountry(cloudflareCountry)
            : CountryByIp();
    }

    private string? CountryByIp()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            return null;
        }

        if (IpAddress.StartsWith("127.", StringComparison.OrdinalIgnoreCase)
            || IpAddress.StartsWith("10.", StringComparison.OrdinalIgnoreCase))
        {
            return "RU";
        }

        return "US";
    }

    private static string? NormalizeCountry(string? country)
    {
        return string.IsNullOrWhiteSpace(country) ? null : country.Trim().ToUpperInvariant();
    }

    private static string DetectDevice(string userAgent)
    {
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return "tablet";
        }

        return userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            ? "mobile"
            : "desktop";
    }

    private static string? DetectBrowser(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
        {
            return "edge";
        }

        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
        {
            return "firefox";
        }

        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("CriOS/", StringComparison.OrdinalIgnoreCase))
        {
            return "chrome";
        }

        return userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase)
            ? "safari"
            : "unknown";
    }
}
