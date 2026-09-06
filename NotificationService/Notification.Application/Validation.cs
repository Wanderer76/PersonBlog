using Infrastructure.Services;
using Notification.Application.Notifications;
using Shared.Utils;

namespace Notification.Application;

internal static class Validation
{
    public static Result Id(Guid value, string name) => value == Guid.Empty
        ? Result.Failure(name, "Identifier must not be empty.")
        : Result.Success();

    public static async Task<Result<Guid>> User(ICurrentUserService currentUser,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = (await currentUser.GetCurrentUserAsync()).UserId;
        return id == Guid.Empty
            ? Result<Guid>.Failure(new Error("Unauthorized", "A validated user session is required."))
            : Result<Guid>.Success(id);
    }

    public static Result Checkpoint(InboxCheckpoint? checkpoint)
    {
        if (checkpoint?.Source is null)
            return Result.Failure(nameof(checkpoint), "Inbox checkpoint and source are required.");
        if (string.IsNullOrWhiteSpace(checkpoint.Source.Producer))
            return Result.Failure(nameof(checkpoint.Source.Producer), "Producer is required.");
        if (string.IsNullOrWhiteSpace(checkpoint.ConsumerName))
            return Result.Failure(nameof(checkpoint.ConsumerName), "Consumer name is required.");

        var eventId = Id(checkpoint.Source.EventId, nameof(checkpoint.Source.EventId));
        if (eventId.IsFailure) return eventId;
        return Id(checkpoint.LeaseToken, nameof(checkpoint.LeaseToken));
    }

    public static Result Content(NotificationContent? content)
    {
        if (content?.Target is null || content.Data is null)
            return Result.Failure(nameof(content), "Notification content, target and data are required.");
        if (!Enum.IsDefined(content.Kind) || !Enum.IsDefined(content.Target.Type))
            return Result.Failure(nameof(content), "Unknown notification kind or target type.");

        var businessId = Id(content.BusinessId, nameof(content.BusinessId));
        if (businessId.IsFailure) return businessId;
        var targetId = Id(content.Target.Id, nameof(content.Target.Id));
        if (targetId.IsFailure) return targetId;
        if (content.ActorUserId is { } actor)
        {
            var actorId = Id(actor, nameof(content.ActorUserId));
            if (actorId.IsFailure) return actorId;
        }
        if (string.IsNullOrWhiteSpace(content.TemplateKey))
            return Result.Failure(nameof(content.TemplateKey), "Template key is required.");
        if (content.TemplateVersion < 1)
            return Result.Failure(nameof(content.TemplateVersion), "Template version must be positive.");
        if (content.Kind == NotificationKind.ConferenceInvitation && content.ExpiresAt is null)
            return Result.Failure(nameof(content.ExpiresAt), "An invitation requires an expiry.");

        return Result.Success();
    }
}
