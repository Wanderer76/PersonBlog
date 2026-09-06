using Notification.Application.Notifications;

namespace Notification.Application.Fanout;

public sealed record Campaign(Guid Id, NotificationSource Source, Guid PublicationId, Guid BlogId,
    DateTimeOffset AudienceCutoff, NotificationContent Content);

/// <summary>Snapshot returned by the worker's atomic claim operation.</summary>
public sealed record ClaimedCampaign(Campaign Campaign, Guid LeaseToken, string? Cursor);

public sealed record StartCampaignCommand(InboxCheckpoint Checkpoint, Guid PublicationId,
    Guid BlogId, bool IsPublic, DateTimeOffset AudienceCutoff, NotificationContent Content);

public sealed record CampaignStartResult(Guid? CampaignId, bool Created);
