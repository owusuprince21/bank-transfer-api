using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ApiDemo.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var customerId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetCustomerGroup(customerId));
        }

        await base.OnConnectedAsync();
    }

    public static string GetCustomerGroup(Guid customerId)
    {
        return GetCustomerGroup(customerId.ToString());
    }

    private static string GetCustomerGroup(string customerId)
    {
        return $"customer:{customerId}";
    }
}
