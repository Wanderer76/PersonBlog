using Authentication.Contract.Constants;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Services;
using Notification.Application.Notifications;
using Notification.Contract.Models;
using Shared.Services;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[AuthFilter(Roles.User, Roles.Blogger)]
public sealed class NotificationsController(
    GetNotifications getNotifications,
    CountUnread countUnread,
    MarkRead markRead,
    NotificationCursorProtector cursors,
    IDateTimeManager dateTimeManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<NotificationPageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPageResponse>> List(string? cursor = null, int limit = 50, bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var snapshotAt = dateTimeManager.UtcNow();
        NotificationCursor? boundary = null;
        if (cursor is not null)
        {
            var decoded = cursors.Unprotect(cursor, requirePosition: true);
            if (decoded.IsFailure) return BadRequest(decoded.Errors);
            snapshotAt = decoded.Value.SnapshotAt;
            boundary = new NotificationCursor(decoded.Value.CreatedAt!.Value, decoded.Value.Id!.Value);
        }

        var result = await getNotifications.ExecuteAsync(snapshotAt, boundary, limit, unreadOnly, cancellationToken);

        if (result.IsFailure) return BadRequest(result.Errors);

        var page = result.Value;
        return Ok(new NotificationPageResponse(
            [.. page.Items.Select(Map)],
            page.NextCursor is null ? null : cursors.ProtectPage(page.SnapshotAt, page.NextCursor),
            cursors.ProtectSnapshot(page.SnapshotAt)));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken cancellationToken = default)
    {
        var result = await countUnread.ExecuteAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(new UnreadCountResponse(result.Value))
            : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await markRead.ExecuteAsync(id, dateTimeManager.UtcNow(), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Errors);
        return result.Value ? NoContent() : NotFound();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> ReadAll(MarkAllNotificationsReadRequest request, CancellationToken cancellationToken = default)
    {
        var snapshot = cursors.Unprotect(request.Snapshot, requirePosition: false);
        if (snapshot.IsFailure) return BadRequest(snapshot.Errors);
        var result = await markRead.AllAsync(snapshot.Value.SnapshotAt, dateTimeManager.UtcNow(), cancellationToken);

        return result.IsSuccess
            ? Ok(new MarkAllNotificationsReadResponse(result.Value))
            : BadRequest(result.Errors);
    }

    private static NotificationItemResponse Map(NotificationItem item) => new(
        item.Id,
        item.Content.Kind.ToString(),
        item.Content.BusinessId,
        item.Content.ActorUserId,
        new NotificationTargetResponse(item.Content.Target.Type.ToString(), item.Content.Target.Id),
        item.Content.TemplateKey,
        item.Content.TemplateVersion,
        item.Content.Data,
        item.CreatedAt,
        item.ReadAt,
        item.Content.ExpiresAt);
}
