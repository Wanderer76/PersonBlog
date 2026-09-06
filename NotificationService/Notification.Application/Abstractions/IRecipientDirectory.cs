namespace Notification.Application.Abstractions;

public sealed record RecipientPage(IReadOnlyList<Guid> UserIds, string? NextCursor, bool HasMore);

public interface IRecipientDirectory
{
    /// <summary>Active Profile subscribers at page read time, bounded by cutoff; cursor is opaque.</summary>
    Task<RecipientPage> GetRecipientsPageAsync(Guid blogId, string? cursor, int limit,
        DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
