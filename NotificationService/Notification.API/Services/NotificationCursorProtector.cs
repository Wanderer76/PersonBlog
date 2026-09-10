using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Notification.Application.Notifications;

namespace Notification.API.Services;

public sealed record NotificationCursorState(
    DateTimeOffset SnapshotAt,
    DateTimeOffset? CreatedAt = null,
    Guid? Id = null);

public sealed class NotificationCursorProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector(
        "Notification.API.Cursor.v1");

    public string ProtectSnapshot(DateTimeOffset snapshotAt) =>
        Protect(new NotificationCursorState(snapshotAt.ToUniversalTime()));

    public string ProtectPage(DateTimeOffset snapshotAt, NotificationCursor cursor) =>
        Protect(new NotificationCursorState(snapshotAt.ToUniversalTime(),
            cursor.CreatedAt.ToUniversalTime(), cursor.Id));

    public Result<NotificationCursorState> Unprotect(string? value, bool requirePosition)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<NotificationCursorState>.Failure(new Shared.Utils.Error(
                "cursor", "A cursor token is required."));
        try
        {
            var state = JsonSerializer.Deserialize<NotificationCursorState>(_protector.Unprotect(value));
            if (state is null || state.SnapshotAt == default ||
                requirePosition && (state.CreatedAt is null || state.Id is null || state.Id == Guid.Empty))
                return Invalid();
            return state;
        }
        catch (CryptographicException)
        {
            return Invalid();
        }
        catch (JsonException)
        {
            return Invalid();
        }
    }

    private string Protect(NotificationCursorState state) =>
        _protector.Protect(JsonSerializer.Serialize(state));

    private static Result<NotificationCursorState> Invalid() =>
        Result<NotificationCursorState>.Failure(new Shared.Utils.Error(
            "cursor", "The cursor token is invalid or expired."));
}
