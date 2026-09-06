using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

/// <summary>Called by a delivery worker after commit, after rechecking preferences and expiry.</summary>
public interface INotificationDelivery
{
    DeliveryType Channel { get; }

    /// <summary>Use the stable job id as the provider idempotency key. Success does not imply ReadAt.</summary>
    Task DeliverAsync(Guid deliveryJobId, string destinationKey, NotificationItem notification,
        CancellationToken cancellationToken = default);
}