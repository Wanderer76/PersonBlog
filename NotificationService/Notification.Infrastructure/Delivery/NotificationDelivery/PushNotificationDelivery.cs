using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Delivery;

public sealed class PushNotificationDelivery(IPushNotificationSender sender) : INotificationDelivery
{
    public DeliveryType Channel => DeliveryType.Push;

    public Task<Result> DeliverAsync(Guid deliveryJobId, string destinationKey, NotificationItem notification,
        CancellationToken cancellationToken = default) =>
        DeliveryAttempt.Run(deliveryJobId, destinationKey,
            () => sender.SendAsync(deliveryJobId, destinationKey, NotificationDeliveryMessage.From(notification),
                cancellationToken), cancellationToken);
}
