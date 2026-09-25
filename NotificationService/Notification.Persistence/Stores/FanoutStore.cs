using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Shared.Services;
using Shared.Utils;

namespace Notification.Persistence.Stores;

internal sealed class FanoutStore(StoreOperation operation, IDateTimeManager dateTimeManager)
    : NotificationStoreBase(operation, dateTimeManager), IFanoutStore
{
    public Task<Result<CampaignStartResult>> CompleteStartAsync(InboxCheckpoint checkpoint, Campaign? campaign,
        CancellationToken cancellationToken = default) => Operation.Run<CampaignStartResult>(async db =>
        {
            var inbox = await FindInboxAsync(db, checkpoint, cancellationToken);
            if (inbox is null)
                return Result<CampaignStartResult>.Failure(Stale);

            var result = new CampaignStartResult(null, false);
            if (campaign is not null)
            {
                var key = Encode(new { campaign.PublicationId, campaign.Content.Kind });
                var existing = await db.Set<NotificationWork>().SingleOrDefaultAsync(work =>
                    work.Kind == NotificationWorkKind.Fanout && work.DedupKey == key, cancellationToken);
                if (existing is null)
                    db.Add(NotificationWork.CreateCampaign(campaign.Id, key, Encode(campaign), Now));
                result = new CampaignStartResult(existing?.Id ?? campaign.Id, existing is null);
            }

            return inbox.TryComplete(checkpoint.LeaseToken, Now)
                ? Result<CampaignStartResult>.Success(result)
                : Result<CampaignStartResult>.Failure(Stale);
        }, true, cancellationToken);

    public Task<Result> CommitBatchAsync(ClaimedCampaign claim, IReadOnlyList<NotificationDraft> notifications,
        string? nextCursor, bool completed, CancellationToken cancellationToken = default) =>
        Operation.Run(async db =>
        {
            var work = await db.Set<NotificationWork>().SingleOrDefaultAsync(item =>
                item.Id == claim.Campaign.Id && item.Kind == NotificationWorkKind.Fanout &&
                item.Status == NotificationWorkStatus.Processing && item.LeaseToken == claim.LeaseToken &&
                item.LeaseUntil > Now && item.Cursor == claim.Cursor, cancellationToken);
            if (work is null)
                return Result.Failure(Stale);
            if (notifications.Any(notification =>
                    notification.Channels.Any(channel => channel != DeliveryType.InApp)))
                return Result.Failure("Delivery.UnsupportedChannel", "Drafts support InApp only.");

            foreach (var notification in notifications)
                await InsertAsync(db, notification, cancellationToken);

            return work.TryCommitBatch(claim.LeaseToken, claim.Cursor, nextCursor, completed, Now)
                ? Result.Success()
                : Result.Failure("Fanout.InvalidCursor", "The lease must be current and the cursor must advance.");
        }, cancellationToken);
}
