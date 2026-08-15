using System.Net;
using WebPush;
using PushSubscription = WebPush.PushSubscription;

namespace Infrastructure.Services;

public class WebPushNotificationService(
    AppDbContext context,
    IConfiguration configuration,
    ILogger<WebPushNotificationService> logger,
    IServiceProvider serviceProvider)
    : IWebPushNotificationService
{
    public async Task SendNotificationAsync(Guid userId, string title, string message, string url = null,
        string notificationType = "System", Guid? relatedEntityId = null, string tag = null, object[] actions = null,
        object onActionClick = null)
    {
        // 1. Save to Database
        var appNotification = new AppNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Url = url,
            Type = notificationType,
            RelatedEntityId = relatedEntityId
        };
        context.AppNotifications.Add(appNotification);
        await context.SaveChangesAsync();

        // 1.5. Send Live Notification (SignalR)
        try
        {
            using var scope = serviceProvider.CreateScope();
            var liveService = scope.ServiceProvider.GetRequiredService<Application.Abstractions.ILiveNotificationService>();
            await liveService.SendLiveNotificationAsync(userId, appNotification);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send live notification via SignalR");
        }

        // 2. Send Push Notification
        var subscriptions = await context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (!subscriptions.Any()) return;

        foreach (var sub in subscriptions)
        {
            await SendNotificationToEndpointAsync(sub.Endpoint, sub.P256DH, sub.Auth, title, message, url, tag, actions,
                onActionClick);
        }
    }

    public async Task SendNotificationToEndpointAsync(string endpoint, string p256dh, string auth, string title,
        string message, string url = null, string tag = null, object[] actions = null, object onActionClick = null)
    {
        try
        {
            var vapidPublicKey = configuration["VapidDetails:PublicKey"];
            var vapidPrivateKey = configuration["VapidDetails:PrivateKey"];
            var subject = configuration["VapidDetails:Subject"]; // e.g., mailto:support@pharmalink.com

            if (string.IsNullOrEmpty(vapidPublicKey) || string.IsNullOrEmpty(vapidPrivateKey))
            {
                logger.LogWarning("VAPID keys not configured. Cannot send push notification.");
                return;
            }

            var pushSubscription = new PushSubscription(endpoint, p256dh, auth);
            var vapidDetails = new VapidDetails(subject, vapidPublicKey, vapidPrivateKey);
            var webPushClient = new WebPushClient();

            // Setup default action click if not provided
            var finalOnActionClick = onActionClick ?? new
            {
                @default = new
                {
                    operation = "navigateLastFocusedOrOpen",
                    url = url ?? "/"
                }
            };

            var payloadObj = new
            {
                notification = new
                {
                    title,
                    body = message,
                    icon = "/icons/icon-192x192.png",
                    badge = "/icons/icon-72x72.png",
                    tag = tag,
                    renotify = tag != null, // If there's a tag, renotify so it buzzes again when replacing
                    data = new
                    {
                        onActionClick = finalOnActionClick
                    },
                    actions = actions,
                    vibrate = new[] { 100, 50, 100 }
                }
            };

            var payload = JsonSerializer.Serialize(payloadObj,
                new JsonSerializerOptions
                    { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

            await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
        }
        catch (WebPushException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Subscription is no longer valid, remove it from DB
                var invalidSub = await context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
                if (invalidSub != null)
                {
                    context.PushSubscriptions.Remove(invalidSub);
                    await context.SaveChangesAsync();
                }
            }

            logger.LogError(ex, "Failed to send push notification");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push notification");
        }
    }
}