using Notification.Application.Fanout;
using Notification.Application.Notifications;

namespace Notification.Application.Abstractions;

public interface IFanoutStore
{
    /// <summary>
    /// Complete the claimed inbox and insert the campaign in one transaction, checking its lease.
    /// Enforce uniqueness by PublicationId and purpose. A null campaign completes a suppressed event.
    /// </summary>
    Task<Result<CampaignStartResult>> CompleteStartAsync(InboxCheckpoint checkpoint, Campaign? campaign,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// In one transaction check the lease AND expected cursor, upsert notifications and delivery jobs,
    /// advance cursor and mark completed if requested. Reject stale claims without writing anything.
    /// Enforce notification business-key uniqueness; duplicates must not create new delivery jobs.
    /// Release the claim after commit. HTTP calls must not occur inside this transaction.
    /// </summary>
    Task<Result> CommitBatchAsync(ClaimedCampaign claim, IReadOnlyList<NotificationDraft> notifications,
        string? nextCursor, bool completed, CancellationToken cancellationToken = default);
}
