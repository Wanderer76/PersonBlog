using Microsoft.AspNetCore.SignalR;
using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Delivery;

public sealed class InAppNotificationDelivery(
    IHubContext<NotificationHub, INotificationClient> hubContext) : INotificationDelivery
{
    public DeliveryType Channel => DeliveryType.InApp;

    public Task DeliverAsync(Guid deliveryJobId, string destinationKey, NotificationItem notification,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(destinationKey).NotificationCreated(
            NotificationDeliveryMessage.From(notification));
}
