using Application.DTOs.Notification;

namespace Application.Services
{
    public interface INotificationService
    {
        Task SendPoCreatedNotificationAsync(PoNotificationDto notification, string email);
    }
}
