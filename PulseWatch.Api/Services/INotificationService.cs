using PulseWatch.Api.Models;

namespace PulseWatch.Api.Services;

public interface INotificationService
{
    Task SendUptimeAlertAsync(Website website, bool isOnline, string reason);
}
