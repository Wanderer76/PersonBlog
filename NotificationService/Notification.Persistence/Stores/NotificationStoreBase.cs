using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Shared.Services;
using Shared.Utils;

namespace Notification.Persistence.Stores;

internal abstract class NotificationStoreBase(StoreOperation operation, IDateTimeManager dateTimeManager)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected StoreOperation Operation { get; } = operation;
    protected DateTimeOffset Now => dateTimeManager.UtcNow();
    protected static Error Stale => new("Work.StaleLease", "The claim expired, completed or was replaced.");

    protected static string Encode<T>(T value) => JsonSerializer.Serialize(value, Json);
    protected static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value, Json)!;

    protected static string InboxKey(NotificationSource source, string consumer) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            Encode(new[] { source.Producer, source.EventId.ToString("D"), consumer }))));

    protected static NotificationItem Item(UserNotification notification) =>
        new(notification.Id, notification.UserId, notification.Content, notification.CreatedAt, notification.ReadAt);

    protected Task<NotificationWork?> FindInboxAsync(NotificationDbContext db, InboxCheckpoint checkpoint,
        CancellationToken cancellationToken) => db.Set<NotificationWork>().SingleOrDefaultAsync(work =>
        work.Kind == NotificationWorkKind.Inbox &&
        work.DedupKey == InboxKey(checkpoint.Source, checkpoint.ConsumerName) &&
        work.Status == NotificationWorkStatus.Processing &&
        work.LeaseToken == checkpoint.LeaseToken &&
        work.LeaseUntil > Now, cancellationToken);

    protected async Task<CreationResult> InsertAsync(NotificationDbContext db, NotificationDraft draft,
        CancellationToken cancellationToken)
    {
        var existing = await db.Set<UserNotification>().SingleOrDefaultAsync(notification =>
            notification.UserId == draft.UserId && notification.Kind == draft.Content.Kind &&
            notification.BusinessId == draft.Content.BusinessId, cancellationToken);
        existing ??= db.Set<UserNotification>().Local.FirstOrDefault(notification =>
            notification.UserId == draft.UserId && notification.Kind == draft.Content.Kind &&
            notification.BusinessId == draft.Content.BusinessId);
        if (existing is not null)
            return new CreationResult(CreationStatus.Duplicate, existing.Id);

        db.Add(UserNotification.Create(draft.Id, draft.UserId, draft.Source, draft.Content, draft.CreatedAt));
        foreach (var channel in draft.Channels.Distinct())
        {
            var destination = draft.UserId.ToString("D");
            var key = Encode(new { draft.Id, Channel = channel, Destination = destination });
            db.Add(NotificationWork.CreateDelivery(Guid.NewGuid(), key, draft.Id, channel, destination, Now));
        }

        return new CreationResult(CreationStatus.Created, draft.Id);
    }
}
