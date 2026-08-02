using Application.DTOs.Notification;

namespace API.Notification
{
    public interface INotificationService
    {
        Task SendPoCreatedNotificationAsync(PoNotificationDto notification, string email);
    }
}
