using System.Diagnostics;
using PulseWatch.Api.Models;
namespace PulseWatch.Api.Services;

public class UptimeCheckerService
{
    private readonly HttpClient _httpClient;
    public UptimeCheckerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // Set a timeout for the HTTP requests
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

        // logic
        return new UptimeCheck
        {
            WebsiteId = website.Id,
            IsOnline = isOnline,
            StatusCode = statusCode,
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            ErrorMessage = errorMessage,
            CheckedAt = DateTime.UtcNow
        };
    }
}