using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Messaging;

public sealed class VideoProcessingFailedV1Handler(INotificationWorkStore workStore)
    : IEventHandler<VideoProcessingFailedV1>
{
    public const string ConsumerName = "notification.video-processing-failed.v1";

    public async Task Handle(IMessageContext<VideoProcessingFailedV1> context)
    {
        var result = await HandleAsync(context.Message);
        if (result.IsFailure) throw new InvalidOperationException($"Video failure intake failed: {result.Errors[0].Key}");
    }

    public Task<Result> HandleAsync(VideoProcessingFailedV1 message, CancellationToken cancellationToken = default)
    {
        if (message.SchemaVersion != VideoProcessingFailedV1.CurrentSchemaVersion ||
            message.Producer != VideoProcessingFailedV1.ProducerName || message.EventId == Guid.Empty ||
            message.PostId == Guid.Empty || message.RecipientUserId == Guid.Empty ||
            message.VideoMetadataId == Guid.Empty || message.ProcessingAttemptId == Guid.Empty ||
            string.IsNullOrWhiteSpace(message.ErrorCode))
            return Task.FromResult(Result.Failure("VideoProcessing.InvalidContract", "The video failure event is invalid."));

        var content = new NotificationContent(NotificationKind.VideoProcessingFailed,
            message.ProcessingAttemptId, null, new NotificationTarget(NotificationTargetType.Post, message.PostId),
            "video.processing.failed", 1,
            new Dictionary<string, string> { ["errorCode"] = message.ErrorCode.Trim() },
            message.OccurredAt.ToUniversalTime());
        return workStore.EnqueueAsync(new NotificationIngress(
            new NotificationSource(message.Producer, message.EventId), ConsumerName, content,
            RecipientUserId: message.RecipientUserId), cancellationToken);
    }
}
