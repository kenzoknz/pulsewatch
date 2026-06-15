using System.Diagnostics;
using Microsoft.Playwright;
using PulseWatch.Api.DTOs;

namespace PulseWatch.Api.Services;

public interface IDeepCheckService
{
    Task<DeepCheckResponseDto> RunCheckAsync(string url, CancellationToken cancellationToken = default);
}

public class DeepCheckService : IDeepCheckService, IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(2, 2);
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(18);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly object _lock = new();

    private async Task EnsureBrowserAsync()
    {
        if (_browser != null) return;
        lock (_lock)
        {
            if (_browser != null) return;
        }
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" }
        });
    }

    public async Task<DeepCheckResponseDto> RunCheckAsync(string url, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        var result = new DeepCheckResponseDto();
        var sw = Stopwatch.StartNew();

        try
        {
            await EnsureBrowserAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
            });

            try
            {
                var page = await context.NewPageAsync();

                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = (float)_timeout.TotalMilliseconds
                });

                sw.Stop();

                result.IsOnline = response != null && response.Ok;
                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.PageTitle = await page.TitleAsync();
                result.StatusCode = response?.Status;

                var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    FullPage = false
                });
                result.ScreenshotBase64 = Convert.ToBase64String(screenshotBytes);
            }
            finally
            {
                await context.CloseAsync();
            }
        }
        catch (TimeoutException)
        {
            sw.Stop();
            result.IsOnline = false;
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            result.ErrorMessage = "Timeout exceeded the time limit for loading page.";
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.IsOnline = false;
            result.ResponseTimeMs = sw.ElapsedMilliseconds;
            result.ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _semaphore.Release();
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}