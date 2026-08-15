using Domain.Entities;

namespace Application.Abstractions;

public interface ILiveNotificationService
{
    Task SendLiveNotificationAsync(Guid userId, AppNotification notification);
}
