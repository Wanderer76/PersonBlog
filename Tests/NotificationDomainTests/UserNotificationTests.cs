using Notification.Domain.Entities;

namespace NotificationDomainTests;

public sealed class UserNotificationTests
{
    [Fact]
    public void UsesExplicitIdentityAndTimeAndCanBeViewedRepeatedly()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);
        var notification = UserNotification.Create(id, userId, "payload", [1, 2], createdAt);

        Assert.Equal(id, notification.Id);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(createdAt, notification.CreatedAt);
        Assert.Equal(TimeSpan.Zero, notification.CreatedAt.Offset);
        Assert.Equal(notification.CreatedAt, notification.ChangedAt);
        Assert.All(notification.NotificationTypes, link => Assert.Equal(id, link.UserNotificationId));
        Assert.False(notification.IsViewed);

        var viewedAt = createdAt.AddMinutes(5);
        notification.MarkAsViewed(viewedAt);
        notification.MarkAsViewed(viewedAt.AddMinutes(1));

        Assert.True(notification.IsViewed);
        Assert.Equal(viewedAt, notification.ChangedAt);
        Assert.Equal(TimeSpan.Zero, notification.ChangedAt.Offset);
        Assert.Equal(createdAt, notification.CreatedAt);
    }
}
