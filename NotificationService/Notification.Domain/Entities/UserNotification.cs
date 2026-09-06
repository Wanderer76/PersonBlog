namespace Notification.Domain.Entities;

public class UserNotification : INotificationEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public bool IsViewed { get; private set; }
    public string Payload { get; private set; } = null!;
    public List<UserNotificationTypes> NotificationTypes { get; private set; } = [];

    private UserNotification()
    {

    }

    private UserNotification(Guid id, Guid userId, string payload, List<UserNotificationTypes> notificationTypesIds, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
        ChangedAt = CreatedAt;
        UserId = userId;
        Payload = payload;
        IsViewed = false;
        NotificationTypes = notificationTypesIds.ToList();
    }

    public static UserNotification Create(Guid id, Guid userId, string payload, IEnumerable<long> notificationTypesIds, DateTimeOffset createdAt)
    {
        return new UserNotification(id, userId, payload, [.. notificationTypesIds.Select(x => new UserNotificationTypes(id, x))], createdAt);
    }

    public void MarkAsViewed(DateTimeOffset changedAt)
    {
        if (IsViewed)
            return;

        IsViewed = true;
        ChangedAt = changedAt;
    }
}
