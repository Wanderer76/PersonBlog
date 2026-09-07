using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Shared.Services;
using Shared.Utils;

namespace Notification.Persistence.Stores;

internal sealed class NotificationWorkStore(StoreOperation operation, IDateTimeManager dateTimeManager)
    : NotificationStoreBase(operation, dateTimeManager), INotificationWorkStore
{
    public Task<Result> EnqueueAsync(NotificationIngress message, CancellationToken cancellationToken = default)
    {
        var validation = NotificationIngressValidation.Validate(message);
        if (validation.IsFailure)
            return Task.FromResult(validation);

        return Operation.Run(async db =>
        {
            var key = InboxKey(message.Source, message.ConsumerName);
            if (!await db.Set<NotificationWork>().AnyAsync(work =>
                    work.Kind == NotificationWorkKind.Inbox && work.DedupKey == key, cancellationToken))
                db.Add(NotificationWork.CreateInbox(Guid.NewGuid(), key, Encode(message), Now));
            return Result.Success();
        }, cancellationToken);
    }

    private Task<Result<T?>> ClaimAsync<T>(NotificationWorkKind kind, TimeSpan leaseDuration,
        Func<NotificationDbContext, NotificationWork, Task<T>> project, CancellationToken cancellationToken)
        where T : class => Operation.Run<T?>(async db =>
        {
            if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromHours(1))
                return Result<T?>.Failure(new Error(
                    "leaseDuration", "Lease must be positive and at most one hour."));

            var now = Now;
            var rows = await db.Set<NotificationWork>().FromSqlInterpolated($"""
                SELECT * FROM "Notification"."NotificationWork"
                WHERE "Kind" = {(int)kind} AND
                    (("Status" = {(int)NotificationWorkStatus.Pending} AND "AvailableAt" <= {now}) OR
                     ("Status" = {(int)NotificationWorkStatus.Processing} AND "LeaseUntil" <= {now}))
                ORDER BY "AvailableAt", "Id"
                LIMIT 1 FOR UPDATE SKIP LOCKED
                """).ToListAsync(cancellationToken);
            var work = rows.FirstOrDefault();
            if (work is null)
                return Result<T?>.Success(null);
            if (!work.TryClaim(Guid.NewGuid(), now, leaseDuration))
                throw new InvalidOperationException("A locked notification work item could not be claimed.");
            return Result<T?>.Success(await project(db, work));
        }, true, cancellationToken);

    public Task<Result<ClaimedInbox?>> ClaimInboxAsync(TimeSpan leaseDuration,
        CancellationToken cancellationToken = default) => ClaimAsync(NotificationWorkKind.Inbox, leaseDuration,
        (db, work) =>
        {
            var message = Decode<NotificationIngress>(work.Payload!);
            return Task.FromResult(new ClaimedInbox(work.Id,
                new InboxCheckpoint(message.Source, message.ConsumerName, work.LeaseToken!.Value), message));
        }, cancellationToken);

    public Task<Result<ClaimedCampaign?>> ClaimCampaignAsync(TimeSpan leaseDuration,
        CancellationToken cancellationToken = default) => ClaimAsync(NotificationWorkKind.Fanout, leaseDuration,
        (db, work) => Task.FromResult(new ClaimedCampaign(
            Decode<Campaign>(work.Payload!), work.LeaseToken!.Value, work.Cursor)), cancellationToken);

    public Task<Result<ClaimedDelivery?>> ClaimDeliveryAsync(TimeSpan leaseDuration,
        CancellationToken cancellationToken = default) => ClaimAsync(NotificationWorkKind.Delivery, leaseDuration,
        async (db, work) =>
        {
            var notification = await db.Set<UserNotification>().SingleAsync(
                item => item.Id == work.NotificationId, cancellationToken);
            return new ClaimedDelivery(work.Id, work.LeaseToken!.Value, work.Channel!.Value,
                work.DestinationKey!, Item(notification));
        }, cancellationToken);

    public Task<Result> CompleteDeliveryAsync(Guid id, Guid leaseToken,
        CancellationToken cancellationToken = default) => Operation.Run(async db =>
        {
            var work = await db.Set<NotificationWork>().SingleOrDefaultAsync(item =>
                item.Id == id && item.Kind == NotificationWorkKind.Delivery, cancellationToken);
            return work is not null && work.TryComplete(leaseToken, Now)
                ? Result.Success()
                : Result.Failure(Stale);
        }, cancellationToken);

    public Task<Result> FailAsync(NotificationWorkKind kind, Guid id, Guid leaseToken, string errorCode,
        TimeSpan retryDelay, int maxAttempts, CancellationToken cancellationToken = default) =>
        Operation.Run(async db =>
        {
            if (retryDelay < TimeSpan.Zero || retryDelay > TimeSpan.FromDays(1) || maxAttempts < 1)
                return Result.Failure("retry", "Invalid retry settings.");

            var work = await db.Set<NotificationWork>().SingleOrDefaultAsync(item =>
                item.Id == id && item.Kind == kind, cancellationToken);
            if (work is null)
                return Result.Failure(Stale);
            return work.TryFail(leaseToken, errorCode, Now, retryDelay, maxAttempts)
                ? Result.Success()
                : Result.Failure(Stale);
        }, cancellationToken);
}
