using Notification.Application.Abstractions;
using Notification.Application.Notifications;

namespace Notification.Application.Fanout;

public sealed class StartCampaign(IFanoutStore store)
{
    public async Task<Result<CampaignStartResult>> ExecuteAsync(StartCampaignCommand? command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return Result<CampaignStartResult>.Failure(
                new Shared.Utils.Error(nameof(command), "Command is required."));

        var checkpoint = Validation.Checkpoint(command.Checkpoint);
        if (checkpoint.IsFailure) return Result<CampaignStartResult>.Failure(checkpoint.Errors);
        var publicationId = Validation.Id(command.PublicationId, nameof(command.PublicationId));
        if (publicationId.IsFailure) return Result<CampaignStartResult>.Failure(publicationId.Errors);
        var blogId = Validation.Id(command.BlogId, nameof(command.BlogId));
        if (blogId.IsFailure) return Result<CampaignStartResult>.Failure(blogId.Errors);
        var contentValidation = Validation.Content(command.Content);
        if (contentValidation.IsFailure) return Result<CampaignStartResult>.Failure(contentValidation.Errors);
        if (command.Content.Kind != NotificationKind.PostPublished ||
            command.Content.BusinessId != command.PublicationId || command.Content.ActorUserId is null)
            return Result<CampaignStartResult>.Failure(new Shared.Utils.Error(nameof(command),
                "A publication campaign requires its publication business key and author."));
        var campaign = command.IsPublic
            ? new Campaign(Guid.NewGuid(), command.Checkpoint.Source, command.PublicationId,
                command.BlogId, command.AudienceCutoff, command.Content)
            : null;
        return await store.CompleteStartAsync(command.Checkpoint, campaign, cancellationToken);
    }
}
