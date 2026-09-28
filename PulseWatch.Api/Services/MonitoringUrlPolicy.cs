using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PulseWatch.Api.Services;

public interface IMonitoringUrlPolicy
{
    MonitoringUrlValidationResult ValidateSyntax(string rawUrl);

    Task<MonitoringUrlValidationResult> ValidateAsync(
        string rawUrl,
        CancellationToken cancellationToken = default);

    Task<MonitoringUrlValidationResult> ValidateAsync(
        Uri uri,
        CancellationToken cancellationToken = default);

    MonitoringHeaderValidationResult ValidateCustomHeaders(string? rawHeadersJson);
}

public sealed record MonitoringUrlValidationResult(
    bool IsValid,
    string Code,
    string Message,
    Uri? Uri = null)
{
    public static MonitoringUrlValidationResult Valid(Uri uri) =>
        new(true, string.Empty, string.Empty, uri);

    public static MonitoringUrlValidationResult Invalid(string code, string message) =>
        new(false, code, message);
}

public sealed record MonitoringHeaderValidationResult(
    bool IsValid,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Headers = null)
{
    public static MonitoringHeaderValidationResult Valid(IReadOnlyDictionary<string, string> headers) =>
        new(true, string.Empty, string.Empty, headers);

    public static MonitoringHeaderValidationResult Invalid(string code, string message) =>
        new(false, code, message);
}

public sealed class MonitoringUrlPolicy : IMonitoringUrlPolicy
{
    private static readonly string[] ForbiddenHeaderPrefixes = ["Proxy-"];
    private static readonly HashSet<string> ForbiddenHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Connection",
        "Cookie",
        "Host",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private readonly MonitoringUrlOptions _options;

    public MonitoringUrlPolicy(IOptions<UptimeMonitoringOptions> options)
    {
        _options = options.Value.UrlSecurity;
    }

    public MonitoringUrlValidationResult ValidateSyntax(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return MonitoringUrlValidationResult.Invalid("invalid_url", "A monitoring URL is required.");

        if (rawUrl.Length > _options.MaxUrlLength || rawUrl.Any(char.IsControl) || rawUrl.Any(char.IsWhiteSpace))
            return MonitoringUrlValidationResult.Invalid("invalid_url", "The monitoring URL is invalid.");

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return MonitoringUrlValidationResult.Invalid("unsupported_scheme", "Only HTTP and HTTPS URLs are allowed.");

        if (string.IsNullOrWhiteSpace(uri.Host) || uri.UserInfo.Length > 0 || !string.IsNullOrEmpty(uri.Fragment))
            return MonitoringUrlValidationResult.Invalid("invalid_url", "The monitoring URL is invalid.");

        if (!uri.IsDefaultPort && !_options.AllowedPorts.Contains(uri.Port))
            return MonitoringUrlValidationResult.Invalid("unsupported_port", "This monitoring port is not allowed.");

        var normalized = Normalize(uri);
        return MonitoringUrlValidationResult.Valid(normalized);
    }

    public async Task<MonitoringUrlValidationResult> ValidateAsync(
        string rawUrl,
        CancellationToken cancellationToken = default)
    {
        var syntax = ValidateSyntax(rawUrl);
        return syntax.IsValid
            ? await ValidateAsync(syntax.Uri!, cancellationToken)
            : syntax;
    }

    public async Task<MonitoringUrlValidationResult> ValidateAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        var syntax = ValidateSyntax(uri.ToString());
        if (!syntax.IsValid)
            return syntax;

        if (_options.AllowPrivateNetworks)
            return syntax;

        if (IPAddress.TryParse(syntax.Uri!.Host, out var literalAddress))
            return IsBlockedAddress(literalAddress)
                ? BlockedDestination()
                : syntax;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.DnsResolutionTimeoutSeconds)));

            var addresses = await Dns.GetHostAddressesAsync(syntax.Uri.Host, timeout.Token);
            if (addresses.Length == 0 || addresses.Any(IsBlockedAddress))
                return BlockedDestination();

            return syntax;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MonitoringUrlValidationResult.Invalid("dns_timeout", "The monitoring target could not be validated.");
        }
        catch (SocketException)
        {
            return MonitoringUrlValidationResult.Invalid("dns_failed", "The monitoring target could not be validated.");
        }
    }

    public MonitoringHeaderValidationResult ValidateCustomHeaders(string? rawHeadersJson)
    {
        if (string.IsNullOrWhiteSpace(rawHeadersJson))
            return MonitoringHeaderValidationResult.Valid(new Dictionary<string, string>());

        if (rawHeadersJson.Length > _options.MaxCustomHeadersLength)
            return MonitoringHeaderValidationResult.Invalid("invalid_custom_headers", "Custom headers are too large.");

        try
        {
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(rawHeadersJson);
            if (headers == null || headers.Count > _options.MaxCustomHeaderCount)
                return MonitoringHeaderValidationResult.Invalid("invalid_custom_headers", "Custom headers are invalid.");

            foreach (var (name, value) in headers)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) ||
                    name.Any(char.IsControl) || value.Contains('\r') || value.Contains('\n') ||
                    ForbiddenHeaders.Contains(name) || ForbiddenHeaderPrefixes.Any(name.StartsWith) ||
                    !_options.AllowedCustomHeaders.Contains(name))
                    return MonitoringHeaderValidationResult.Invalid("invalid_custom_headers", "One or more custom headers are not allowed.");
            }

            return MonitoringHeaderValidationResult.Valid(headers);
        }
        catch (JsonException)
        {
            return MonitoringHeaderValidationResult.Invalid("invalid_custom_headers", "Custom headers must be a JSON object of string values.");
        }
    }

    private static Uri Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty
        };

        return builder.Uri;
    }

    private MonitoringUrlValidationResult BlockedDestination() =>
        MonitoringUrlValidationResult.Invalid("blocked_destination", "This monitoring target is not allowed.");

    private static bool IsBlockedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return MatchesPrefix(bytes, [0, 0, 0, 0], 8) ||
                   MatchesPrefix(bytes, [10, 0, 0, 0], 8) ||
                   MatchesPrefix(bytes, [100, 64, 0, 0], 10) ||
                   MatchesPrefix(bytes, [127, 0, 0, 0], 8) ||
                   MatchesPrefix(bytes, [169, 254, 0, 0], 16) ||
                   MatchesPrefix(bytes, [172, 16, 0, 0], 12) ||
                   MatchesPrefix(bytes, [192, 0, 0, 0], 24) ||
                   MatchesPrefix(bytes, [192, 0, 2, 0], 24) ||
                   MatchesPrefix(bytes, [192, 168, 0, 0], 16) ||
                   MatchesPrefix(bytes, [198, 18, 0, 0], 15) ||
                   MatchesPrefix(bytes, [198, 51, 100, 0], 24) ||
                   MatchesPrefix(bytes, [203, 0, 113, 0], 24) ||
                   MatchesPrefix(bytes, [224, 0, 0, 0], 4) ||
                   MatchesPrefix(bytes, [240, 0, 0, 0], 4);
        }

        return address.Equals(IPAddress.IPv6Loopback) ||
               MatchesPrefix(bytes, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 128) ||
               MatchesPrefix(bytes, [0xfc, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 7) ||
               MatchesPrefix(bytes, [0xfe, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 10) ||
               MatchesPrefix(bytes, [0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 8) ||
               MatchesPrefix(bytes, [0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 32);
    }

    private static bool MatchesPrefix(byte[] address, byte[] prefix, int prefixLength)
    {
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (address[i] != prefix[i])
                return false;
        }

        return remainingBits == 0 ||
               (address[fullBytes] & (byte)(0xff << (8 - remainingBits))) ==
               (prefix[fullBytes] & (byte)(0xff << (8 - remainingBits)));
    }
}