using System.Diagnostics;
using PulseWatch.Api.Models;
namespace PulseWatch.Api.Services;

public class UptimeCheckerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UptimeCheckerService> _logger;

    public UptimeCheckerService(HttpClient httpClient, ILogger<UptimeCheckerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UptimeCheck> CheckWebsiteAsync(Website website)
    {
        var stopwatch = Stopwatch.StartNew();
        bool isOnline = false;
        int? statusCode = null;
        string? errorMessage = null;

        try
        {
            using var response = await _httpClient.GetAsync(website.Url);
            isOnline = response.IsSuccessStatusCode;
            statusCode = (int)response.StatusCode;
            errorMessage = isOnline ? null : response.ReasonPhrase;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
        }

        var responseTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url}, IsOnline={IsOnline}, StatusCode={StatusCode}, ResponseTimeMs={ResponseTimeMs}, Error={ErrorMessage}",
            website.Id,
            website.Url,
            isOnline,
            statusCode,
            responseTimeMs,
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