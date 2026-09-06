using Microsoft.AspNetCore.SignalR;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Notification.Infrastructure.Delivery;

namespace NotificationIntegrationTests;

public sealed class NotificationDeliveryTests
{
    [Fact]
    public async Task InAppDeliverySendsNotificationToRequestedSignalRUser()
    {
        var clients = new HubClientsStub();
        var delivery = new InAppNotificationDelivery(new HubContextStub(clients));
        var notification = CreateNotification();

        await delivery.DeliverAsync(Guid.NewGuid(), "user-42", notification);

        Assert.Equal(DeliveryType.InApp, delivery.Channel);
        Assert.Equal("user-42", clients.RequestedUser);
        Assert.Equal(notification.Id, clients.TargetClient.Message?.NotificationId);
        Assert.Equal(notification.Content.TemplateKey, clients.TargetClient.Message?.TemplateKey);
    }

    [Fact]
    public async Task PushDeliveryForwardsStableJobIdAndDestinationToProvider()
    {
        var sender = new PushSenderStub();
        var delivery = new PushNotificationDelivery(sender);
        var jobId = Guid.NewGuid();
        var notification = CreateNotification();

        await delivery.DeliverAsync(jobId, "device-7", notification);

        Assert.Equal(DeliveryType.Push, delivery.Channel);
        Assert.Equal(jobId, sender.IdempotencyKey);
        Assert.Equal("device-7", sender.DestinationKey);
        Assert.Equal(notification.Id, sender.Message?.NotificationId);
    }

    private static NotificationItem CreateNotification()
    {
        var content = new NotificationContent(
            NotificationKind.CommentReply,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new NotificationTarget(NotificationTargetType.Comment, Guid.NewGuid()),
            "comment.reply",
            1,
            new Dictionary<string, string> { ["author"] = "Alex" },
            DateTimeOffset.UtcNow);
        return new NotificationItem(Guid.NewGuid(), Guid.NewGuid(), content, DateTimeOffset.UtcNow, null);
    }

    private sealed class PushSenderStub : IPushNotificationSender
    {
        public Guid IdempotencyKey { get; private set; }
        public string? DestinationKey { get; private set; }
        public NotificationDeliveryMessage? Message { get; private set; }

        public Task SendAsync(Guid idempotencyKey, string destinationKey,
            NotificationDeliveryMessage notification, CancellationToken cancellationToken = default)
        {
            IdempotencyKey = idempotencyKey;
            DestinationKey = destinationKey;
            Message = notification;
            return Task.CompletedTask;
        }
    }

    private sealed class NotificationClientStub : INotificationClient
    {
        public NotificationDeliveryMessage? Message { get; private set; }

        public Task NotificationCreated(NotificationDeliveryMessage notification)
        {
            Message = notification;
            return Task.CompletedTask;
        }
    }

    private sealed class HubContextStub(HubClientsStub clients)
        : IHubContext<NotificationHub, INotificationClient>
    {
        public IHubClients<INotificationClient> Clients { get; } = clients;
        public IGroupManager Groups => null!;
    }

    private sealed class HubClientsStub : IHubClients<INotificationClient>
    {
        public string? RequestedUser { get; private set; }
        public NotificationClientStub TargetClient { get; } = new();
        public INotificationClient All => throw new NotSupportedException();

        public INotificationClient User(string userId)
        {
            RequestedUser = userId;
            return TargetClient;
        }

        public INotificationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) =>
            throw new NotSupportedException();

        public INotificationClient Client(string connectionId) => throw new NotSupportedException();
        public INotificationClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public INotificationClient Group(string groupName) => throw new NotSupportedException();

        public INotificationClient GroupExcept(string groupName,
            IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();

        public INotificationClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public INotificationClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }
}
