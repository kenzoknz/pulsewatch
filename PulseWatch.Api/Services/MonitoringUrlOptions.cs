namespace PulseWatch.Api.Services;

public class MonitoringUrlOptions
{
    public int MaxUrlLength { get; set; } = 2048;

    public int MaxRedirects { get; set; } = 5;

    public int MaxResponseBodyBytes { get; set; } = 1_048_576;

    public int DnsResolutionTimeoutSeconds { get; set; } = 5;

    public int[] AllowedPorts { get; set; } = [80, 443];

    public int MaxCustomHeaderCount { get; set; } = 20;

    public int MaxCustomHeadersLength { get; set; } = 2048;

    public bool AllowPrivateNetworks { get; set; }

    public HashSet<string> AllowedCustomHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "Accept-Language",
        "Cache-Control",
        "Content-Type",
        "User-Agent"
    };
}