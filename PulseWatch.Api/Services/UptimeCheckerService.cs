using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PulseWatch.Api.Models;
namespace PulseWatch.Api.Services;

public class UptimeCheckerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UptimeCheckerService> _logger;
    private readonly UptimeMonitoringOptions _options;
    private readonly IMonitoringUrlPolicy _urlPolicy;

    public UptimeCheckerService(
        HttpClient httpClient,
        ILogger<UptimeCheckerService> logger,
        IOptions<UptimeMonitoringOptions> options,
        IMonitoringUrlPolicy urlPolicy)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _urlPolicy = urlPolicy;
    }

    public async Task<UptimeCheck> CheckWebsiteAsync(Website website)
    {
        var stopwatch = Stopwatch.StartNew();
        bool isOnline = false;
        int? statusCode = null;
        string? errorMessage = null;

        var urlValidation = await _urlPolicy.ValidateAsync(website.Url);
        if (!urlValidation.IsValid)
        {
            return CreateFailedCheck(website, urlValidation.Message);
        }

        var headersValidation = _urlPolicy.ValidateCustomHeaders(website.CustomHeadersJson);
        if (!headersValidation.IsValid)
        {
            return CreateFailedCheck(website, headersValidation.Message);
        }

        var maxAttempts = Math.Max(1, _options.MaxRetries);
        var attempt = 0;

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
                var result = await SendWithSafeRedirectsAsync(
                    urlValidation.Uri!,
                    method,
                    headersValidation.Headers!,
                    website.ResponseBodyKeyword,
                    CancellationToken.None);

                statusCode = result.StatusCode;
                isOnline = result.IsOnline;
                errorMessage = result.ErrorMessage;

                if (!isOnline && attempt < maxAttempts && result.IsRetryable)
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
                else if (!isOnline && !result.IsRetryable)
                {
                    _logger.LogWarning(
                        "[UptimeCheck] WebsiteId={WebsiteId}, Url={Url} was rejected by monitoring policy. Error={ErrorMessage}",
                        website.Id,
                        website.Url,
                        errorMessage);

                    break;
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

    private async Task<CheckAttemptResult> SendWithSafeRedirectsAsync(
        Uri initialUri,
        HttpMethod method,
        IReadOnlyDictionary<string, string> headers,
        string? responseBodyKeyword,
        CancellationToken cancellationToken)
    {
        var currentUri = initialUri;
        var currentMethod = method;
        var redirectCount = 0;

        while (true)
        {
            using var request = new HttpRequestMessage(currentMethod, currentUri);
            foreach (var (key, value) in headers)
                request.Headers.TryAddWithoutValidation(key, value);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (IsRedirect(response.StatusCode))
            {
                if (redirectCount >= _options.UrlSecurity.MaxRedirects || response.Headers.Location == null)
                    return CheckAttemptResult.Failed((int)response.StatusCode, "The monitoring redirect chain is invalid.", false);

                var redirectUri = new Uri(currentUri, response.Headers.Location);
                var redirectValidation = await _urlPolicy.ValidateAsync(redirectUri, cancellationToken);
                if (!redirectValidation.IsValid)
                    return CheckAttemptResult.Failed((int)response.StatusCode, redirectValidation.Message, false);

                currentUri = redirectValidation.Uri!;
                redirectCount++;

                if (response.StatusCode == HttpStatusCode.SeeOther ||
                    ((response.StatusCode == HttpStatusCode.Moved || response.StatusCode == HttpStatusCode.Found) &&
                     currentMethod != HttpMethod.Get && currentMethod != HttpMethod.Head))
                {
                    currentMethod = HttpMethod.Get;
                }

                continue;
            }

            if (!response.IsSuccessStatusCode)
                return CheckAttemptResult.Failed((int)response.StatusCode, response.ReasonPhrase, true);

            if (currentMethod != HttpMethod.Head && !string.IsNullOrWhiteSpace(responseBodyKeyword))
            {
                var body = await ReadBodyLimitedAsync(response.Content, _options.UrlSecurity.MaxResponseBodyBytes, cancellationToken);
                if (body == null)
                    return CheckAttemptResult.Failed((int)response.StatusCode, "The response body is too large.", false);

                if (!body.Contains(responseBodyKeyword, StringComparison.OrdinalIgnoreCase))
                    return CheckAttemptResult.Failed(
                        (int)response.StatusCode,
                        $"Response body does not contain the required keyword: \"{responseBodyKeyword}\"",
                        true);
            }

            return CheckAttemptResult.Success((int)response.StatusCode);
        }
    }

    private static async Task<string?> ReadBodyLimitedAsync(
        HttpContent content,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > maxBytes)
            return null;

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];

        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0)
                break;

            if (buffer.Length + read > maxBytes)
                return null;

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved or HttpStatusCode.Found or
        HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private static UptimeCheck CreateFailedCheck(Website website, string errorMessage) => new()
    {
        WebsiteId = website.Id,
        IsOnline = false,
        ErrorMessage = errorMessage,
        CheckedAt = DateTime.UtcNow
    };

    private sealed record CheckAttemptResult(
        bool IsOnline,
        int? StatusCode,
        string? ErrorMessage,
        bool IsRetryable)
    {
        public static CheckAttemptResult Success(int statusCode) => new(true, statusCode, null, false);

        public static CheckAttemptResult Failed(int statusCode, string? errorMessage, bool isRetryable) =>
            new(false, statusCode, errorMessage, isRetryable);
    }
}