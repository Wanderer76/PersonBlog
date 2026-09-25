using Infrastructure.Middleware;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Infrastructure.Delivery;

[AuthFilter]
public sealed class NotificationHub : Hub<INotificationClient>;
