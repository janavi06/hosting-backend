using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Restaurant_Menu.Hubs
{
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Call this from the client once you know the waiter’s userId so
        /// you join the correct group for notifications.
        /// </summary>
        public Task JoinGroup(int waiterUserId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"waiter_{waiterUserId}");
    }
}
