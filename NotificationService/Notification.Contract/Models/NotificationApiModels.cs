namespace Notification.Contract.Models;

public sealed record NotificationTargetResponse(string Type, Guid Id);

public sealed record NotificationItemResponse(
    Guid Id,
    string Kind,
    Guid BusinessId,
    Guid? ActorUserId,
    NotificationTargetResponse Target,
    string TemplateKey,
    int TemplateVersion,
    IReadOnlyDictionary<string, string> Data,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    DateTimeOffset? ExpiresAt);

public sealed record NotificationPageResponse(
    IReadOnlyList<NotificationItemResponse> Items,
    string? NextCursor,
    string Snapshot);

public sealed record UnreadCountResponse(long Count);

public sealed record MarkAllNotificationsReadRequest(string Snapshot);

public sealed record MarkAllNotificationsReadResponse(int Count);

public sealed record NotificationPreferenceResponse(string Kind, string Channel, bool Enabled);

public sealed record UpdateNotificationPreferencesRequest(
    IReadOnlyList<NotificationPreferenceResponse> Preferences);
