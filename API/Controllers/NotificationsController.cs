using Application.Abstractions;

namespace API.Controllers;

[Authorize]
public class NotificationsController(AppDbContext context, IWebPushNotificationService webPushService)
    : BaseApiController
{
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        // Check if subscription already exists for this endpoint
        var existingSub = await context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

        if (existingSub != null)
        {
            // Update keys in case they changed, and assign to current user
            existingSub.P256DH = request.Keys.P256dh;
            existingSub.Auth = request.Keys.Auth;
            existingSub.UserId = userId;
        }
        else
        {
            context.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256DH = request.Keys.P256dh,
                Auth = request.Keys.Auth
            });
        }

        await context.SaveChangesAsync();

        // Send a welcome notification
        await webPushService.SendNotificationToEndpointAsync(
            request.Endpoint,
            request.Keys.P256dh,
            request.Keys.Auth,
            "تم التفعيل بنجاح!",
            "أنت الآن تتلقى الإشعارات المباشرة من PharmaLink."
        );

        return Ok(new { message = "Subscription saved successfully." });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        var existingSub = await context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

        if (existingSub != null)
        {
            context.PushSubscriptions.Remove(existingSub);
            await context.SaveChangesAsync();
        }

        return Ok(new { message = "Unsubscribed successfully." });
    }
}

public class PushSubscriptionRequest
{
    public string Endpoint { get; set; }
    public PushSubscriptionKeys Keys { get; set; }
}

public class PushSubscriptionKeys
{
    public string P256dh { get; set; }
    public string Auth { get; set; }
}

public class UnsubscribeRequest
{
    public string Endpoint { get; set; }
}