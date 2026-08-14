using System.Text.Json;
using Application.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace Infrastructure.Services;

public class WebPushNotificationService : IWebPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(AppDbContext context, IConfiguration configuration, ILogger<WebPushNotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, string url = null)
    {
        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (!subscriptions.Any()) return;

        foreach (var sub in subscriptions)
        {
            await SendNotificationToEndpointAsync(sub.Endpoint, sub.P256DH, sub.Auth, title, message, url);
        }
    }

    public async Task SendNotificationToEndpointAsync(string endpoint, string p256dh, string auth, string title, string message, string url = null)
    {
        try
        {
            var vapidPublicKey = _configuration["VapidDetails:PublicKey"];
            var vapidPrivateKey = _configuration["VapidDetails:PrivateKey"];
            var subject = _configuration["VapidDetails:Subject"]; // e.g., mailto:support@pharmalink.com

            if (string.IsNullOrEmpty(vapidPublicKey) || string.IsNullOrEmpty(vapidPrivateKey))
            {
                _logger.LogWarning("VAPID keys not configured. Cannot send push notification.");
                return;
            }

            var pushSubscription = new WebPush.PushSubscription(endpoint, p256dh, auth);
            var vapidDetails = new VapidDetails(subject, vapidPublicKey, vapidPrivateKey);
            var webPushClient = new WebPushClient();

            var payload = JsonSerializer.Serialize(new
            {
                notification = new
                {
                    title,
                    body = message,
                    icon = "/icons/icon-192x192.png",
                    badge = "/icons/icon-72x72.png",
                    data = new { url = url ?? "/" },
                    vibrate = new[] { 100, 50, 100 }
                }
            });

            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
        }
        catch (WebPushException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Subscription is no longer valid, remove it from DB
                var invalidSub = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
                if (invalidSub != null)
                {
                    _context.PushSubscriptions.Remove(invalidSub);
                    await _context.SaveChangesAsync();
                }
            }
            _logger.LogError(ex, "Failed to send push notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification");
        }
    }
}
