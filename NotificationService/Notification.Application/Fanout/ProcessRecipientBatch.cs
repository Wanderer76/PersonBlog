using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Application.Preferences;
using Notification.Domain.Entities;

namespace Notification.Application.Fanout;

public sealed class ProcessRecipientBatch(IFanoutStore store, IRecipientDirectory directory,
    INotificationPreferenceStore preferences)
{
    public async Task<Result> ExecuteAsync(ClaimedCampaign? claim, DateTimeOffset now, int limit = 500,
        CancellationToken cancellationToken = default)
    {
        if (claim is null)
            return Result.Failure(nameof(claim), "Claimed campaign is required.");
        if (claim.Campaign is null || claim.Campaign.Id == Guid.Empty || claim.Campaign.BlogId == Guid.Empty ||
            claim.LeaseToken == Guid.Empty)
            return Result.Failure(nameof(claim), "Campaign, blog and lease identifiers are required.");
        var contentValidation = Validation.Content(claim.Campaign.Content);
        if (contentValidation.IsFailure) return contentValidation;
        if (limit is < 1 or > 500)
            return Result.Failure(nameof(limit), "Limit must be between 1 and 500.");

        var campaign = claim.Campaign;

        if (campaign.Content.ExpiresAt <= now)
        {
            return await store.CommitBatchAsync(claim, [], claim.Cursor, true, cancellationToken);
        }

        // One bounded page per invocation. The worker owns retries/backoff and lease renewal.
        var pageResult = await directory.GetRecipientsPageAsync(campaign.BlogId, claim.Cursor, limit,
            campaign.AudienceCutoff, cancellationToken);
        if (pageResult.IsFailure) return Result.Failure(pageResult.Errors);
        var page = pageResult.Value;
        if (page.UserIds.Count > limit || page.UserIds.Any(x => x == Guid.Empty) ||
            (page.HasMore && (string.IsNullOrWhiteSpace(page.NextCursor) || page.NextCursor == claim.Cursor)))
            return Result.Failure(nameof(page),
                "Recipient directory returned an invalid page or a non-advancing cursor.");

        var drafts = new List<NotificationDraft>();
        foreach (var userId in page.UserIds.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NotificationPolicy.IsSuppressed(campaign.Content, userId, now)) continue;
            var settings = await preferences.GetAsync(userId, cancellationToken);
            if (settings.IsFailure) return Result.Failure(settings.Errors);
            if (!NotificationPreferenceResolver.IsEnabled(campaign.Content.Kind, DeliveryType.InApp, settings.Value)) continue;
            drafts.Add(new NotificationDraft(Guid.NewGuid(), userId, campaign.Source,
                campaign.Content, now, [DeliveryType.InApp]));
        }

        return await store.CommitBatchAsync(claim, drafts, page.NextCursor, !page.HasMore, cancellationToken);
    }
}
