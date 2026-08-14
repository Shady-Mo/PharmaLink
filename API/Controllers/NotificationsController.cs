using Application.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebPushNotificationService _webPushService;

        public NotificationsController(AppDbContext context, IWebPushNotificationService webPushService)
        {
            _context = context;
            _webPushService = webPushService;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            {
                return Unauthorized();
            }

            // Check if subscription already exists for this endpoint
            var existingSub = await _context.PushSubscriptions
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
                _context.PushSubscriptions.Add(new PushSubscription
                {
                    UserId = userId,
                    Endpoint = request.Endpoint,
                    P256DH = request.Keys.P256dh,
                    Auth = request.Keys.Auth
                });
            }

            await _context.SaveChangesAsync();

            // Send a welcome notification
            await _webPushService.SendNotificationToEndpointAsync(
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
            var existingSub = await _context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

            if (existingSub != null)
            {
                _context.PushSubscriptions.Remove(existingSub);
                await _context.SaveChangesAsync();
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
}
