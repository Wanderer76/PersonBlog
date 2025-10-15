using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notification.Domain.Entities;

public class UserNotification : INotificationEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsViewed { get; private set; }
    public string Payload {  get; private set; }
    public List<UserNotificationTypes> NotificationTypes { get; private set; }

    private UserNotification()
    {

    }

    public UserNotification(Guid userId, string payload, IEnumerable<long> notificationTypesIds)
    {
        Id = GuidService.GetNewGuid();
        CreatedAt = DateTimeService.Now();
        UserId = userId;
        Payload = payload;
        IsViewed = false;
        NotificationTypes = notificationTypesIds.Select(x => new UserNotificationTypes(Id, x)).ToList();
    }

    public Result MarkAsViewed()
    {
        IsViewed = true;
        return Result.Success();
    }
}

public class UserNotificationTypes
{
    public Guid UserNotificationId { get; private set; }
    public long NotificationTypeId { get; private set; }

    [ForeignKey(nameof(UserNotificationId))]
    public UserNotification UserNotification { get; private set; }

    [ForeignKey(nameof(NotificationTypeId))]
    public NotificationType NotificationType { get; private set; }

    private UserNotificationTypes()
    {

    }


    public UserNotificationTypes(Guid userNotificationId, long notificationTypeId)
    {
        UserNotificationId = userNotificationId;
        NotificationTypeId = notificationTypeId;
    }
}

public class NotificationType : INotificationEntity
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public DeliveryType DeliveryType { get; private set; }
}

public enum DeliveryType
{
    InApp,
    Push,
    Email
}
