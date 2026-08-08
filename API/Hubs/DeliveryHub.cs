using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    public class DeliveryHub(IDeliveryDriverService driverService) : Hub
    {
        public async Task UpdateLocation(string userIdStr, double longitude, double latitude)
        {

            if (Guid.TryParse(userIdStr, out Guid userId))
            {
                await driverService.UpdateLocationAsync(userId, longitude, latitude);
            }
        }
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;

            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var role = user.GetRoleName();


                if (role == AppRoles.Pharmacist)
                {
                    var branchIds = user.GetBranchIds();

                    foreach (var branchId in branchIds)
                    {
                        string groupName = $"Branch_{branchId}";

                        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                    }
                }

            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
