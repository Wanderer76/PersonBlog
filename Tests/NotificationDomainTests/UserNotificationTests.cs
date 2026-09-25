using Notification.Domain.Entities;

namespace NotificationDomainTests;

public sealed class UserNotificationTests
{
    [Fact]
    public void CreatesTypedNotificationAndNormalizesTimesToUtc()
    {
        var now = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.FromHours(5));
        var source = new NotificationSource("comments", Guid.NewGuid());
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var content = Content(now) with { ExpiresAt = now.AddDays(1) };
        var notification = UserNotification.Create(id, userId, source, content, now);

        Assert.Equal(id, notification.Id);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(source, notification.Source);
        Assert.Equal(content.Kind, notification.Kind);
        Assert.Equal(content.BusinessId, notification.BusinessId);
        Assert.Equal(content.Target, notification.Content.Target);
        Assert.Equal(content.TemplateKey, notification.Content.TemplateKey);
        Assert.Equal(now.ToUniversalTime(), notification.CreatedAt);
        Assert.Equal(TimeSpan.Zero, notification.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, notification.Content.OccurredAt.Offset);
        Assert.Equal(TimeSpan.Zero, notification.Content.ExpiresAt!.Value.Offset);
        Assert.Null(notification.ReadAt);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkReadPreservesFirstReadTime()
    {
        var now = DateTimeOffset.UtcNow;
        var notification = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), new("comments", Guid.NewGuid()), Content(now), now);
        var readAt = now.AddMinutes(5).ToOffset(TimeSpan.FromHours(5));
        notification.MarkRead(readAt);
        notification.MarkRead(readAt.AddMinutes(1));

        Assert.True(notification.IsRead);
        Assert.Equal(readAt.ToUniversalTime(), notification.ReadAt);
        Assert.Equal(TimeSpan.Zero, notification.ReadAt!.Value.Offset);
        Assert.Equal(now, notification.CreatedAt);
    }

    [Fact]
    public void CreationTakesImmutableSnapshotOfTemplateData()
    {
        var now = DateTimeOffset.UtcNow;
        var data = new Dictionary<string, string> { ["author"] = "Alice" };
        var content = Content(now) with { Data = data };
        var notification = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), new("comments", Guid.NewGuid()), content, now);
        data["author"] = "Changed";

        Assert.Equal("Alice", notification.Content.Data["author"]);
        var exposed = Assert.IsAssignableFrom<IDictionary<string, string>>(notification.Content.Data);
        Assert.Throws<NotSupportedException>(() => exposed["author"] = "Changed");
    }

    [Fact]
    public void ExpiryHasInclusiveBoundaryAndDoesNotChangeReadState()
    {
        var now = DateTimeOffset.UtcNow;
        var notification = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), new("comments", Guid.NewGuid()),
            Content(now) with { ExpiresAt = now.AddMinutes(1) }, now);
        Assert.False(notification.IsExpired(now));
        Assert.True(notification.IsExpired(now.AddMinutes(1)));
        Assert.Null(notification.ReadAt);
        var permanent = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), new("comments", Guid.NewGuid()), Content(now), now);
        Assert.False(permanent.IsExpired(now.AddYears(10)));
    }

    private static NotificationContent Content(DateTimeOffset now) => new(NotificationKind.CommentReply,
        Guid.NewGuid(), Guid.NewGuid(), new(NotificationTargetType.Comment, Guid.NewGuid()),
        "comment.reply", 1, new Dictionary<string, string>(), now);
}
