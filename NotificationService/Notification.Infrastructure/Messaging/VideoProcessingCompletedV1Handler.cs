using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Messaging;

public sealed class VideoProcessingCompletedV1Handler(INotificationWorkStore workStore)
    : IEventHandler<VideoProcessingCompletedV1>
{
    public const string ConsumerName = "notification.video-processing-completed.v1";

    public async Task Handle(IMessageContext<VideoProcessingCompletedV1> context)
    {
        var result = await HandleAsync(context.Message);
        if (result.IsFailure) throw new InvalidOperationException($"Video completion intake failed: {result.Errors[0].Key}");
    }

    public Task<Result> HandleAsync(VideoProcessingCompletedV1 message, CancellationToken cancellationToken = default)
    {
        if (message.SchemaVersion != VideoProcessingCompletedV1.CurrentSchemaVersion ||
            message.Producer != VideoProcessingCompletedV1.ProducerName || message.EventId == Guid.Empty ||
            message.PostId == Guid.Empty || message.RecipientUserId == Guid.Empty ||
            message.VideoMetadataId == Guid.Empty || message.ProcessingAttemptId == Guid.Empty)
            return Task.FromResult(Result.Failure("VideoProcessing.InvalidContract", "The video completion event is invalid."));

        var content = new NotificationContent(NotificationKind.VideoProcessingCompleted,
            message.ProcessingAttemptId, null, new NotificationTarget(NotificationTargetType.Post, message.PostId),
            "video.processing.completed", 1, new Dictionary<string, string>(), message.OccurredAt.ToUniversalTime());
        return workStore.EnqueueAsync(new NotificationIngress(
            new NotificationSource(message.Producer, message.EventId), ConsumerName, content,
            RecipientUserId: message.RecipientUserId), cancellationToken);
    }
}
