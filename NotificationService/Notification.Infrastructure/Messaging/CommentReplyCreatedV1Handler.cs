using Comments.Contracts.Events;
using MessageBus.EventHandler;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Messaging;

public sealed class CommentReplyCreatedV1Handler(INotificationWorkStore workStore)
    : IEventHandler<CommentReplyCreatedV1>
{
    public const string ConsumerName = "notification.comment-reply-created.v1";

    public async Task Handle(IMessageContext<CommentReplyCreatedV1> context)
    {
        var result = await HandleAsync(context.Message);
        if (result.IsFailure) throw new InvalidOperationException($"Comment reply intake failed: {result.Errors[0].Key}");
    }

    public Task<Result> HandleAsync(CommentReplyCreatedV1 message, CancellationToken cancellationToken = default)
    {
        if (message.SchemaVersion != CommentReplyCreatedV1.CurrentSchemaVersion ||
            message.Producer != CommentReplyCreatedV1.ProducerName || message.EventId == Guid.Empty ||
            message.CommentId == Guid.Empty || message.ParentCommentId == Guid.Empty || message.PostId == Guid.Empty ||
            message.ActorUserId == Guid.Empty || message.RecipientUserId == Guid.Empty)
            return Task.FromResult(Result.Failure("CommentReply.InvalidContract", "The comment reply event is invalid."));

        var content = new NotificationContent(NotificationKind.CommentReply, message.CommentId,
            message.ActorUserId, new NotificationTarget(NotificationTargetType.Comment, message.CommentId),
            "comment.reply", 1,
            new Dictionary<string, string> { ["postId"] = message.PostId.ToString() },
            message.OccurredAt.ToUniversalTime());
        return workStore.EnqueueAsync(new NotificationIngress(
            new NotificationSource(message.Producer, message.EventId), ConsumerName, content,
            RecipientUserId: message.RecipientUserId), cancellationToken);
    }
}
