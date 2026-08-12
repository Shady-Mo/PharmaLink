namespace Application.Services;

public interface IWhatsAppMessageService
{
    Task SendMessageAsync(string phoneNumber, string message);
}
