using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Delivery;

public sealed class PushNotificationDelivery(IPushNotificationSender sender) : INotificationDelivery
{
    public DeliveryType Channel => DeliveryType.Push;

    public Task DeliverAsync(Guid deliveryJobId, string destinationKey, NotificationItem notification,
        CancellationToken cancellationToken = default) =>
        sender.SendAsync(deliveryJobId, destinationKey, NotificationDeliveryMessage.From(notification),
            cancellationToken);
}
