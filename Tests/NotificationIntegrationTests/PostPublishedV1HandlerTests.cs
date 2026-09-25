using Blog.Contracts.Events;
using Notification.Application.Abstractions;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Notification.Infrastructure.Messaging;

namespace NotificationIntegrationTests;

public sealed class PostPublishedV1HandlerTests
{
    [Fact]
    public async Task MapsPublicEventToDurableFanoutIngress()
    {
        var store = new WorkStoreStub();
        var handler = new PostPublishedV1Handler(store);
        var publishedAt = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.FromHours(5));
        var message = CreateEvent(publishedAt);

        var result = await handler.HandleAsync(message);

        Assert.True(result.IsSuccess);
        var ingress = Assert.IsType<NotificationIngress>(store.Message);
        Assert.Equal(message.EventId, ingress.Source.EventId);
        Assert.Equal(PostPublishedV1Handler.ConsumerName, ingress.ConsumerName);
        Assert.Equal(message.PublicationId, ingress.PublicationId);
        Assert.Equal(message.BlogId, ingress.BlogId);
        Assert.Equal(publishedAt.ToUniversalTime(), ingress.AudienceCutoff);
        Assert.True(ingress.IsPublic);
        Assert.Equal(NotificationKind.PostPublished, ingress.Content.Kind);
        Assert.Equal(message.PostId, ingress.Content.Target.Id);
        Assert.Equal(message.Title, ingress.Content.Data["title"]);
    }

    [Fact]
    public async Task MapsNonPublicEventAsSuppressedCampaign()
    {
        var store = new WorkStoreStub();
        var handler = new PostPublishedV1Handler(store);
        var message = CreateEvent(DateTimeOffset.UtcNow) with
        {
            Audience = PostPublicationAudience.Private
        };

        var result = await handler.HandleAsync(message);

        Assert.True(result.IsSuccess);
        Assert.False(store.Message!.IsPublic);
    }

    [Fact]
    public async Task RejectsUnsupportedSchemaBeforeStorage()
    {
        var store = new WorkStoreStub();
        var handler = new PostPublishedV1Handler(store);
        var message = CreateEvent(DateTimeOffset.UtcNow) with { SchemaVersion = 2 };

        var result = await handler.HandleAsync(message);

        Assert.True(result.IsFailure);
        Assert.Equal("PostPublished.InvalidContract", Assert.Single(result.Errors).Key);
        Assert.Null(store.Message);
    }

    private static PostPublishedV1 CreateEvent(DateTimeOffset publishedAt) => new()
    {
        EventId = Guid.NewGuid(),
        OccurredAt = publishedAt,
        PostId = Guid.NewGuid(),
        BlogId = Guid.NewGuid(),
        AuthorUserId = Guid.NewGuid(),
        PublicationId = Guid.NewGuid(),
        PublishedAt = publishedAt,
        Title = "Published post",
        Audience = PostPublicationAudience.Public
    };

    private sealed class WorkStoreStub : INotificationWorkStore
    {
        public NotificationIngress? Message { get; private set; }

        public Task<Result> EnqueueAsync(NotificationIngress message, CancellationToken cancellationToken = default)
        {
            Message = message;
            return Task.FromResult(Result.Success());
        }

        public Task<Result<ClaimedInbox?>> ClaimInboxAsync(TimeSpan leaseDuration,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<ClaimedCampaign?>> ClaimCampaignAsync(TimeSpan leaseDuration,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<ClaimedDelivery?>> ClaimDeliveryAsync(TimeSpan leaseDuration,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result> CompleteDeliveryAsync(Guid id, Guid leaseToken,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result> FailAsync(NotificationWorkKind kind, Guid id, Guid leaseToken, string errorCode,
            TimeSpan retryDelay, int maxAttempts, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
