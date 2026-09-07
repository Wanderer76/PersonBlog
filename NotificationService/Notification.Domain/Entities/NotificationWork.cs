namespace Notification.Domain.Entities;

public enum NotificationWorkKind { Inbox, Fanout, Delivery }
public enum NotificationWorkStatus { Pending, Processing, Completed, Failed }

/// <summary>A durable notification job. State transitions require ownership of a live lease.</summary>
public sealed class NotificationWork : INotificationEntity
{
    public Guid Id { get; private set; }
    public NotificationWorkKind Kind { get; private set; }
    public string DedupKey { get; private set; } = null!;
    public string? Payload { get; private set; }
    public string? Cursor { get; private set; }
    public NotificationWorkStatus Status { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public Guid? NotificationId { get; private set; }
    public DeliveryType? Channel { get; private set; }
    public string? DestinationKey { get; private set; }

    private NotificationWork() { }

    public static NotificationWork CreateInbox(Guid id, string dedupKey, string payload, DateTimeOffset now) =>
        Create(id, NotificationWorkKind.Inbox, dedupKey, payload, now);

    public static NotificationWork CreateCampaign(Guid id, string dedupKey, string payload, DateTimeOffset now) =>
        Create(id, NotificationWorkKind.Fanout, dedupKey, payload, now);

    public static NotificationWork CreateDelivery(Guid id, string dedupKey, Guid notificationId,
        DeliveryType channel, string destinationKey, DateTimeOffset now)
    {
        var work = Create(id, NotificationWorkKind.Delivery, dedupKey, null, now);
        work.NotificationId = notificationId;
        work.Channel = channel;
        work.DestinationKey = destinationKey;
        return work;
    }

    private static NotificationWork Create(Guid id, NotificationWorkKind kind, string key, string? payload,
        DateTimeOffset now) => new()
    {
        Id = id, Kind = kind, DedupKey = key, Payload = payload, AvailableAt = now.ToUniversalTime()
    };

    public bool OwnsLease(Guid leaseToken, DateTimeOffset now) =>
        Status == NotificationWorkStatus.Processing && LeaseToken == leaseToken && LeaseUntil > now;

    public bool TryClaim(Guid leaseToken, DateTimeOffset now, TimeSpan leaseDuration)
    {
        if (leaseToken == Guid.Empty || leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromHours(1))
            return false;
        if (!((Status == NotificationWorkStatus.Pending && AvailableAt <= now) ||
              (Status == NotificationWorkStatus.Processing && LeaseUntil <= now)))
            return false;
        Status = NotificationWorkStatus.Processing;
        LeaseToken = leaseToken;
        LeaseUntil = now.ToUniversalTime().Add(leaseDuration);
        Attempts++;
        return true;
    }

    public bool TryComplete(Guid leaseToken, DateTimeOffset now)
    {
        if (!OwnsLease(leaseToken, now)) return false;
        Status = NotificationWorkStatus.Completed;
        ReleaseLease();
        LastError = null;
        return true;
    }

    public bool TryCommitBatch(Guid leaseToken, string? expectedCursor, string? nextCursor, bool completed,
        DateTimeOffset now)
    {
        if (Kind != NotificationWorkKind.Fanout || Cursor != expectedCursor ||
            (!completed && (string.IsNullOrWhiteSpace(nextCursor) || nextCursor == expectedCursor)) ||
            !TryComplete(leaseToken, now))
            return false;
        Cursor = nextCursor;
        Status = completed ? NotificationWorkStatus.Completed : NotificationWorkStatus.Pending;
        AvailableAt = now.ToUniversalTime();
        Attempts = 0;
        return true;
    }

    public bool TryFail(Guid leaseToken, string errorCode, DateTimeOffset now, TimeSpan retryDelay, int maxAttempts)
    {
        if (retryDelay < TimeSpan.Zero || retryDelay > TimeSpan.FromDays(1) || maxAttempts < 1 ||
            !OwnsLease(leaseToken, now))
            return false;
        Status = Attempts >= maxAttempts ? NotificationWorkStatus.Failed : NotificationWorkStatus.Pending;
        AvailableAt = now.ToUniversalTime().Add(retryDelay);
        ReleaseLease();
        LastError = errorCode.Length <= 200 ? errorCode : errorCode[..200];
        return true;
    }

    private void ReleaseLease()
    {
        LeaseToken = null;
        LeaseUntil = null;
    }
}
