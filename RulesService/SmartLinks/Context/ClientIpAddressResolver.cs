using System.Net;
using SmartLinks.Contracts;

namespace RulesService.SmartLinks.Context;

internal static class ClientIpAddressResolver
{
    public static string? Resolve(RuleHandlerRequest request)
    {
        return FirstForwardedFor(request.Headers)
            ?? Header(request.Headers, "X-Real-IP")
            ?? ForwardedHeader(request.Headers)
            ?? Normalize(request.IpAddress);
    }

    private static string? FirstForwardedFor(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("X-Forwarded-For", out var value))
        {
            return null;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .FirstOrDefault(ip => ip is not null);
    }

    private static string? Header(IReadOnlyDictionary<string, string> headers, string name)
    {
        return headers.TryGetValue(name, out var value) ? Normalize(value) : null;
    }

    private static string? ForwardedHeader(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Forwarded", out var value))
        {
            return null;
        }

        var firstEntry = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (firstEntry is null)
        {
            return null;
        }

        foreach (var part in firstEntry.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2 && string.Equals(pair[0], "for", StringComparison.OrdinalIgnoreCase))
            {
                return Normalize(pair[1]);
            }
        }

        return null;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Trim().Trim('"');
        if (candidate.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        candidate = StripBrackets(candidate);
        if (IPAddress.TryParse(candidate, out var ipAddress))
        {
            return Normalize(ipAddress);
        }

        var withoutPort = StripPort(candidate);
        return IPAddress.TryParse(withoutPort, out ipAddress) ? Normalize(ipAddress) : null;
    }

    private static string StripBrackets(string value)
    {
        var end = value.IndexOf(']');
        if (!value.StartsWith("[", StringComparison.Ordinal) || end <= 0)
        {
            return value;
        }

        return value[1..end];
    }

    private static string StripPort(string value)
    {
        var colonIndex = value.LastIndexOf(':');
        return colonIndex > 0 && value.IndexOf(':') == colonIndex
            ? value[..colonIndex]
            : value;
    }

    private static string Normalize(IPAddress ipAddress)
    {
        return ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4().ToString() : ipAddress.ToString();
    }
}
