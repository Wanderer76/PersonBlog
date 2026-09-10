using Blog.Contracts.Events;
using MessageBus.EventHandler;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Messaging;

public sealed class PostPublishedV1Handler(INotificationWorkStore workStore) : IEventHandler<PostPublishedV1>
{
    public const string ConsumerName = "notification.post-published.v1";

    public async Task Handle(IMessageContext<PostPublishedV1> context)
    {
        var result = await HandleAsync(context.Message);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"PostPublishedV1 intake failed: {result.Errors[0].Key}");
        }
    }

    public Task<Result> HandleAsync(PostPublishedV1 message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.SchemaVersion != PostPublishedV1.CurrentSchemaVersion ||
            !string.Equals(message.Producer, PostPublishedV1.ProducerName, StringComparison.Ordinal) ||
            message.EventId == Guid.Empty || message.PostId == Guid.Empty || message.BlogId == Guid.Empty ||
            message.AuthorUserId == Guid.Empty || message.PublicationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(message.Title))
        {
            return Task.FromResult(Result.Failure(
                "PostPublished.InvalidContract", "The publication event is invalid or unsupported."));
        }

        var content = new NotificationContent(
            NotificationKind.PostPublished,
            message.PublicationId,
            message.AuthorUserId,
            new NotificationTarget(NotificationTargetType.Post, message.PostId),
            "post.published",
            1,
            new Dictionary<string, string> { ["title"] = message.Title.Trim() },
            message.OccurredAt.ToUniversalTime());
        var ingress = new NotificationIngress(
            new NotificationSource(message.Producer, message.EventId),
            ConsumerName,
            content,
            PublicationId: message.PublicationId,
            BlogId: message.BlogId,
            IsPublic: message.Audience == PostPublicationAudience.Public,
            AudienceCutoff: message.PublishedAt.ToUniversalTime());
        return workStore.EnqueueAsync(ingress, cancellationToken);
    }
}
