using Microsoft.Extensions.Options;
using PulseWatch.Api.Services;
using Xunit;

namespace PulseWatch.Api.Tests;

public class MonitoringUrlPolicyTests
{
    private readonly MonitoringUrlPolicy _policy = new(Options.Create(new UptimeMonitoringOptions
    {
        UrlSecurity = new MonitoringUrlOptions()
    }));

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/resource")]
    [InlineData("javascript:alert(1)")]
    [InlineData("relative/path")]
    public void ValidateSyntax_RejectsUnsupportedOrRelativeUrls(string url)
    {
        var result = _policy.ValidateSyntax(url);

        Assert.False(result.IsValid);
        Assert.Contains(result.Code, new[] { "unsupported_scheme", "invalid_url" });
    }

    [Theory]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://169.254.169.254")]
    [InlineData("http://192.168.1.10")]
    [InlineData("http://[::1]")]
    public async Task ValidateAsync_RejectsBlockedIpLiterals(string url)
    {
        var result = await _policy.ValidateAsync(url);

        Assert.False(result.IsValid);
        Assert.Equal("blocked_destination", result.Code);
    }

    [Fact]
    public async Task ValidateAsync_RejectsLocalHostResolution()
    {
        var result = await _policy.ValidateAsync("http://localhost");

        Assert.False(result.IsValid);
        Assert.Equal("blocked_destination", result.Code);
    }

    [Fact]
    public void ValidateSyntax_NormalizesSchemeAndHost()
    {
        var result = _policy.ValidateSyntax("HTTPS://Example.COM/health?check=1");

        Assert.True(result.IsValid);
        Assert.Equal("https://example.com/health?check=1", result.Uri!.ToString());
    }

    [Fact]
    public void ValidateSyntax_RejectsCredentialsAndUnsupportedPort()
    {
        var credentials = _policy.ValidateSyntax("https://user:password@example.com");
        var port = _policy.ValidateSyntax("https://example.com:8443");

        Assert.False(credentials.IsValid);
        Assert.Equal("invalid_url", credentials.Code);
        Assert.False(port.IsValid);
        Assert.Equal("unsupported_port", port.Code);
    }

    [Fact]
    public void ValidateCustomHeaders_AcceptsConfiguredSafeHeaders()
    {
        var result = _policy.ValidateCustomHeaders("{\"Accept\":\"application/json\",\"User-Agent\":\"PulseWatch\"}");

        Assert.True(result.IsValid);
        Assert.Equal("application/json", result.Headers!["Accept"]);
    }

    [Theory]
    [InlineData("{\"Authorization\":\"Bearer secret\"}")]
    [InlineData("{\"Host\":\"internal.example\"}")]
    [InlineData("{\"X-Test\":\"value\"}")]
    [InlineData("{\"Accept\":\"line1\\nline2\"}")]
    [InlineData("not-json")]
    public void ValidateCustomHeaders_RejectsUnsafeHeaders(string json)
    {
        var result = _policy.ValidateCustomHeaders(json);

        Assert.False(result.IsValid);
        Assert.Equal("invalid_custom_headers", result.Code);
    }
}