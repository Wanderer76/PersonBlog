using Notification.Application.Abstractions;
using Notification.Application.Preferences;
using Notification.Domain.Entities;

namespace Notification.Application.Notifications;

public sealed record CreateNotificationCommand(InboxCheckpoint Checkpoint, Guid RecipientUserId,
    NotificationContent Content);

public sealed class CreateNotification(INotificationStore store, INotificationPreferenceStore preferences)
{
    // The adapter obtains now from the existing IDateTimeManager.UtcNow().
    public async Task<Result<CreationResult>> ExecuteAsync(CreateNotificationCommand? command, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<CreationResult>.Failure(
                new Shared.Utils.Error(nameof(command), "Command is required."));

        var checkpoint = Validation.Checkpoint(command.Checkpoint);
        if (checkpoint.IsFailure) return Result<CreationResult>.Failure(checkpoint.Errors);
        var recipient = Validation.Id(command.RecipientUserId, nameof(command.RecipientUserId));
        if (recipient.IsFailure) return Result<CreationResult>.Failure(recipient.Errors);
        var contentValidation = Validation.Content(command.Content);
        if (contentValidation.IsFailure) return Result<CreationResult>.Failure(contentValidation.Errors);

        var content = command.Content;
        var suppressed = NotificationPolicy.IsSuppressed(content, command.RecipientUserId, now);
        if (!suppressed)
        {
            var settings = await preferences.GetAsync(command.RecipientUserId, cancellationToken);
            suppressed = !NotificationPreferenceResolver.IsEnabled(content.Kind, DeliveryType.InApp, settings);
        }

        var draft = suppressed ? null : new NotificationDraft(Guid.NewGuid(), command.RecipientUserId,
            command.Checkpoint.Source, content, now, [DeliveryType.InApp]);
        return await store.CompleteCreationAsync(command.Checkpoint, draft, cancellationToken);
    }
}

public static class NotificationPolicy
{
    public static bool IsSuppressed(NotificationContent content, Guid recipientUserId, DateTimeOffset now) =>
        content.ExpiresAt <= now ||
        (content.Kind is NotificationKind.CommentReply or NotificationKind.PostPublished
            or NotificationKind.ConferenceInvitation && content.ActorUserId == recipientUserId);
}
