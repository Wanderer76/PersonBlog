using Notification.Domain.Entities;
using Notification.Application.Abstractions;

namespace Notification.Application.Notifications;

public static class NotificationIngressValidation
{
    public static Result Validate(NotificationIngress? message)
    {
        if (message is null) return Result.Failure("message", "Notification message is required.");
        var checkpoint = Validation.Checkpoint(new InboxCheckpoint(message.Source, message.ConsumerName, Guid.NewGuid()));
        if (checkpoint.IsFailure) return checkpoint;
        if (message.Source.Producer.Length > 200 || message.ConsumerName.Length > 200)
            return Result.Failure("message", "Producer and consumer names must not exceed 200 characters.");
        var content = Validation.Content(message.Content);
        if (content.IsFailure) return content;
        if (message.PublicationId is { } publicationId)
        {
            if (publicationId == Guid.Empty || message.BlogId is null || message.BlogId == Guid.Empty ||
                message.AudienceCutoff is null || message.RecipientUserId is not null ||
                message.Content.Kind != NotificationKind.PostPublished ||
                message.Content.BusinessId != publicationId || message.Content.ActorUserId is null)
                return Result.Failure("message", "Publication requires a blog, cutoff, author and matching publication business key.");
        }
        else if (message.RecipientUserId is null || message.RecipientUserId == Guid.Empty)
            return Result.Failure("message", "A direct notification requires a recipient.");
        return Result.Success();
    }
}
