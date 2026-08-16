namespace Application.Abstractions;

public interface IWebPushNotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, string url = null, string notificationType = "System", Guid? relatedEntityId = null, string tag = null, object[] actions = null, object onActionClick = null);
    Task SendNotificationToEndpointAsync(string endpoint, string p256dh, string auth, string title, string message, string url = null, string tag = null, object[] actions = null, object onActionClick = null);
}
