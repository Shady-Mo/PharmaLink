
using Microsoft.AspNetCore.SignalR;

namespace Application.Hubs
{
    public class InventoryHub: Hub
    {
        public async Task SubscribeToBranch(Guid branchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Branch_{branchId}");
        }

        public async Task UnsubscribeFromBranch(Guid branchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Branch_{branchId}");
        }
    }
}
