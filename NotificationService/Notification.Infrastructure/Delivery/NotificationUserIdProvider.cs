using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Infrastructure.Delivery;

public sealed class NotificationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var value = connection.User?.FindFirstValue(AppClaimTypes.UserId);
        return Guid.TryParse(value, out var userId) && userId != Guid.Empty
            ? userId.ToString("D")
            : null;
    }
}
