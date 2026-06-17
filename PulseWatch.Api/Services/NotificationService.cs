using Microsoft.AspNetCore.SignalR;
using PulseWatch.Api.Data;
using PulseWatch.Api.Hubs;
using PulseWatch.Api.Models;

namespace PulseWatch.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        IEmailService emailService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _emailService = emailService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendUptimeAlertAsync(Website website, bool isOnline, string reason)
    {
        try
        {
            var notification = new InAppNotification
            {
                UserId = website.UserId,
                WebsiteId = website.Id,
                Title = isOnline ? "Website is back Online" : "Website is Down",
                Message = isOnline
                    ? $"'{website.Name}' is back online."
                    : $"'{website.Name}' went offline. Error: {reason}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.InAppNotifications.Add(notification);
            await _db.SaveChangesAsync();
            _logger.LogInformation("In-app notification created for User {UserId}, Website {WebsiteId}", website.UserId, website.Id);

            // Push real-time via SignalR
            await _hubContext.Clients.User(website.UserId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.WebsiteId,
                WebsiteName = website.Name,
                notification.Title,
                notification.Message,
                notification.IsRead,
                notification.CreatedAt
            });

            // Send email if enabled
            if (website.User != null && website.User.EmailNotificationsEnabled && !string.IsNullOrEmpty(website.User.Email))
            {
                var statusText = isOnline ? "ONLINE (UP)" : "OFFLINE (DOWN)";
                var subject = $"[PulseWatch] {website.Name} is {statusText}";
                var body = $"Website {website.Name} ({website.Url}) is {statusText} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.\nReason: {reason}";
                await _emailService.SendEmailAsync(website.User.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notifications for Website {WebsiteId}", website.Id);
        }
    }
}
