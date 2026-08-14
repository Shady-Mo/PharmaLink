namespace Application.Abstractions;

public interface IWebPushNotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, string url = null);
    Task SendNotificationToEndpointAsync(string endpoint, string p256dh, string auth, string title, string message, string url = null);
}
