using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PulseWatch.Api.Models;
namespace PulseWatch.Api.Services;

public class UptimeCheckerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UptimeCheckerService> _logger;
    private readonly UptimeMonitoringOptions _options;

    public UptimeCheckerService(
        HttpClient httpClient,
        ILogger<UptimeCheckerService> logger,
        IOptions<UptimeMonitoringOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<UptimeCheck> CheckWebsiteAsync(Website website)
    {
        var stopwatch = Stopwatch.StartNew();
        bool isOnline = false;
        int? statusCode = null;
        string? errorMessage = null;

        var maxAttempts = Math.Max(1, _options.MaxRetries);
        var attempt = 0;

        // Parse custom headers once, outside the retry loop
        Dictionary<string, string>? customHeaders = null;
        if (!string.IsNullOrWhiteSpace(website.CustomHeadersJson))
        {
            try
            {
                customHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(website.CustomHeadersJson);
            }
            catch (JsonException)
            {
                _logger.LogWarning("[UptimeCheck] WebsiteId={WebsiteId} has invalid CustomHeadersJson, ignoring headers.", website.Id);
            }
        }

        // Determine the HTTP method (default to GET for safety)
        var method = website.HttpMethod?.ToUpperInvariant() switch
        {
            "HEAD" => HttpMethod.Head,
            "POST" => HttpMethod.Post,
            _      => HttpMethod.Get,
        };

        while (attempt < maxAttempts && !isOnline)
        {
            attempt++;

            try
            {
                using var request = new HttpRequestMessage(method, website.Url);

                // Attach custom headers
                if (customHeaders != null)
                {
                    foreach (var (key, value) in customHeaders)
                    {
                        // Use TryAddWithoutValidation so unusual header names don't throw
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }

                using var response = await _httpClient.SendAsync(request);
                statusCode = (int)response.StatusCode;
                isOnline = response.IsSuccessStatusCode;
                errorMessage = isOnline ? null : response.ReasonPhrase;

                // Keyword assertion: only checked when method is not HEAD and response is otherwise online
                if (isOnline && method != HttpMethod.Head && !string.IsNullOrWhiteSpace(website.ResponseBodyKeyword))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (!body.Contains(website.ResponseBodyKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        isOnline = false;
                        errorMessage = $"Response body does not contain the required keyword: \"{website.ResponseBodyKeyword}\"";

                        _logger.LogWarning(
                            "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url}: Keyword assertion FAILED. Expected \"{Keyword}\" in response body.",
                            website.Id, website.Url, website.ResponseBodyKeyword);
                    }
                }

                if (!isOnline && attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url}, Attempt={Attempt}/{MaxAttempts} returned invalid response. StatusCode={StatusCode}, Error={ErrorMessage}. Retrying...",
                        website.Id,
                        website.Url,
                        attempt,
                        maxAttempts,
                        statusCode,
                        errorMessage
                    );
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;

                if (attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        ex,
                        "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url}, Attempt={Attempt}/{MaxAttempts} failed. Retrying...",
                        website.Id,
                        website.Url,
                        attempt,
                        maxAttempts
                    );
                }
            }
        }

        stopwatch.Stop();

        var responseTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url}, Method={Method}, IsOnline={IsOnline}, StatusCode={StatusCode}, ResponseTimeMs={ResponseTimeMs}, Attempts={Attempts}, Error={ErrorMessage}",
            website.Id,
            website.Url,
            method.Method,
            isOnline,
            statusCode,
            responseTimeMs,
            attempt,
            errorMessage
        );

        return new UptimeCheck
        {
            WebsiteId = website.Id,
            IsOnline = isOnline,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            CheckedAt = DateTime.UtcNow
        };
    }
}