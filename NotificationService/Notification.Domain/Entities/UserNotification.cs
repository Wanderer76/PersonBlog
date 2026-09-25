using System.Collections.ObjectModel;

namespace Notification.Domain.Entities;

/// <summary>One recipient's immutable notification and its independently managed read state.</summary>
public sealed class UserNotification : INotificationEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Producer { get; private set; } = null!;
    public Guid EventId { get; private set; }
    public NotificationContent Content { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public NotificationSource Source => new(Producer, EventId);
    public bool IsRead => ReadAt.HasValue;

    private UserNotification() { }

    /// <summary>Creates a notification from validated input; identity and time are supplied by the caller.</summary>
    public static UserNotification Create(Guid id, Guid userId, NotificationSource source,
        NotificationContent content, DateTimeOffset createdAt) => new()
    {
        Id = id,
        UserId = userId,
        Kind = content.Kind,
        BusinessId = content.BusinessId,
        Producer = source.Producer,
        EventId = source.EventId,
        Content = content with
        {
            Data = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(content.Data)),
            OccurredAt = content.OccurredAt.ToUniversalTime(),
            ExpiresAt = content.ExpiresAt?.ToUniversalTime()
        },
        CreatedAt = createdAt.ToUniversalTime()
    };

    public bool IsExpired(DateTimeOffset now) => Content.ExpiresAt <= now;

    /// <summary>Repeated reads preserve the time of the first read.</summary>
    public void MarkRead(DateTimeOffset readAt) => ReadAt ??= readAt.ToUniversalTime();
}
