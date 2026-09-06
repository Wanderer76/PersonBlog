namespace Notification.Domain.Entities;

public class UserNotificationTypes
{
    public Guid UserNotificationId { get; private set; }
    public long NotificationTypeId { get; private set; }

    public UserNotification UserNotification { get; private set; } = null!;

    public NotificationType NotificationType { get; private set; } = null!;

    private UserNotificationTypes()
    {

    }

    public UserNotificationTypes(Guid userNotificationId, long notificationTypeId)
    {
        UserNotificationId = userNotificationId;
        NotificationTypeId = notificationTypeId;
    }
}
