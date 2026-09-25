using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Domain.Entities;

namespace Notification.Application.Abstractions;

/// <summary>A validated source fact. PublicationId selects fanout; otherwise RecipientUserId is required.</summary>
public sealed record NotificationIngress(NotificationSource Source, string ConsumerName,
    NotificationContent Content, Guid? RecipientUserId = null, Guid? PublicationId = null,
    Guid? BlogId = null, bool IsPublic = false, DateTimeOffset? AudienceCutoff = null);

public sealed record ClaimedInbox(Guid Id, InboxCheckpoint Checkpoint, NotificationIngress Message);
public sealed record ClaimedDelivery(Guid Id, Guid LeaseToken, DeliveryType Channel,
    string DestinationKey, NotificationItem Notification);

/// <summary>Durable intake and bounded, atomic work claims. All mutations validate unexpired leases.</summary>
public interface INotificationWorkStore
{
    Task<Result> EnqueueAsync(NotificationIngress message, CancellationToken cancellationToken = default);
    Task<Result<ClaimedInbox?>> ClaimInboxAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<Result<ClaimedCampaign?>> ClaimCampaignAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<Result<ClaimedDelivery?>> ClaimDeliveryAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<Result> CompleteDeliveryAsync(Guid id, Guid leaseToken, CancellationToken cancellationToken = default);
    /// <summary>Release a failed claim with backoff; quarantine after maxAttempts. Store only a safe error code.</summary>
    Task<Result> FailAsync(NotificationWorkKind kind, Guid id, Guid leaseToken, string errorCode,
        TimeSpan retryDelay, int maxAttempts, CancellationToken cancellationToken = default);
}
