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
            //var user = Context.User;

            //if (user != null && user.Identity?.IsAuthenticated == true)
            //{
            //    var role = user.GetRoleName();


            //    if (role == AppRoles.Pharmacist)
            //    {
            //        var branchIds = user.GetBranchIds();

            //        foreach (var branchId in branchIds)
            //        {
            //            string groupName = $"Branch_{branchId}";

            //            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            //        }
            //    }

                
            //}

            var httpContext = Context.GetHttpContext();
            var driverIdStr = httpContext?.Request.Query["userId"].ToString();

            if (!string.IsNullOrEmpty(driverIdStr))
            {
                if (Guid.TryParse(driverIdStr, out Guid userId))
                {
                    await driverService.SetStatustToOnline(userId);
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, driverIdStr);
                Console.WriteLine($"[SignalR] Driver {driverIdStr} joined his group!");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var driverIdStr = httpContext?.Request.Query["userId"].ToString();
            if (Guid.TryParse(driverIdStr, out Guid userId))
            {
                await driverService.SetStatustToOffline(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }


    }
}
