using System.Diagnostics;
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

        while (attempt < maxAttempts && !isOnline)
        {
            attempt++;

            try
            {
                using var response = await _httpClient.GetAsync(website.Url);
                statusCode = (int)response.StatusCode;
                isOnline = response.IsSuccessStatusCode;
                errorMessage = isOnline ? null : response.ReasonPhrase;

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
            "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url}, IsOnline={IsOnline}, StatusCode={StatusCode}, ResponseTimeMs={ResponseTimeMs}, Attempts={Attempts}, Error={ErrorMessage}",
            website.Id,
            website.Url,
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