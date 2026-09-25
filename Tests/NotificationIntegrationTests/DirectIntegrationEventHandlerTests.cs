using Blog.Contracts.Events;
using Comments.Contracts.Events;
using Conference.Contracts.Events;
using Notification.Application.Abstractions;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Domain.Entities;
using Notification.Infrastructure.Messaging;

namespace NotificationIntegrationTests;

public sealed class DirectIntegrationEventHandlerTests
{
    [Fact]
    public async Task MapsVideoCompletion()
    {
        var store = new WorkStoreStub();
        var message = new VideoProcessingCompletedV1
        {
            EventId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid(), BlogId = Guid.NewGuid(), RecipientUserId = Guid.NewGuid(),
            VideoMetadataId = Guid.NewGuid(), ProcessingAttemptId = Guid.NewGuid()
        };

        Assert.True((await new VideoProcessingCompletedV1Handler(store).HandleAsync(message)).IsSuccess);
        Assert.Equal(NotificationKind.VideoProcessingCompleted, store.Message!.Content.Kind);
        Assert.Equal(message.ProcessingAttemptId, store.Message.Content.BusinessId);
        Assert.Equal(message.RecipientUserId, store.Message.RecipientUserId);
    }

    [Fact]
    public async Task MapsVideoFailureWithoutLeakingProviderError()
    {
        var store = new WorkStoreStub();
        var message = new VideoProcessingFailedV1
        {
            EventId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow,
            PostId = Guid.NewGuid(), BlogId = Guid.NewGuid(), RecipientUserId = Guid.NewGuid(),
            VideoMetadataId = Guid.NewGuid(), ProcessingAttemptId = Guid.NewGuid(), ErrorCode = "conversion_failed"
        };

        Assert.True((await new VideoProcessingFailedV1Handler(store).HandleAsync(message)).IsSuccess);
        Assert.Equal(NotificationKind.VideoProcessingFailed, store.Message!.Content.Kind);
        Assert.Equal("conversion_failed", store.Message.Content.Data["errorCode"]);
    }

    [Fact]
    public async Task MapsCommentReply()
    {
        var store = new WorkStoreStub();
        var message = new CommentReplyCreatedV1
        {
            EventId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, CommentId = Guid.NewGuid(),
            ParentCommentId = Guid.NewGuid(), PostId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(),
            RecipientUserId = Guid.NewGuid()
        };

        Assert.True((await new CommentReplyCreatedV1Handler(store).HandleAsync(message)).IsSuccess);
        Assert.Equal(NotificationKind.CommentReply, store.Message!.Content.Kind);
        Assert.Equal(message.ActorUserId, store.Message.Content.ActorUserId);
        Assert.Equal(message.CommentId, store.Message.Content.Target.Id);
    }

    [Fact]
    public async Task MapsConferenceInvitationWithExpiry()
    {
        var store = new WorkStoreStub();
        var occurredAt = DateTimeOffset.UtcNow;
        var message = new ConferenceInvitationCreatedV1
        {
            EventId = Guid.NewGuid(), OccurredAt = occurredAt, InvitationId = Guid.NewGuid(),
            ConferenceId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(), RecipientUserId = Guid.NewGuid(),
            ExpiresAt = occurredAt.AddHours(1)
        };

        Assert.True((await new ConferenceInvitationCreatedV1Handler(store).HandleAsync(message)).IsSuccess);
        Assert.Equal(NotificationKind.ConferenceInvitation, store.Message!.Content.Kind);
        Assert.Equal(message.ExpiresAt, store.Message.Content.ExpiresAt);
        Assert.Equal(message.InvitationId, store.Message.Content.Target.Id);
    }

    private sealed class WorkStoreStub : INotificationWorkStore
    {
        public NotificationIngress? Message { get; private set; }
        public Task<Result> EnqueueAsync(NotificationIngress message, CancellationToken cancellationToken = default)
        {
            Message = message;
            return Task.FromResult(Result.Success());
        }
        public Task<Result<ClaimedInbox?>> ClaimInboxAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<ClaimedCampaign?>> ClaimCampaignAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<ClaimedDelivery?>> ClaimDeliveryAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result> CompleteDeliveryAsync(Guid id, Guid leaseToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result> FailAsync(NotificationWorkKind kind, Guid id, Guid leaseToken, string errorCode,
            TimeSpan retryDelay, int maxAttempts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
