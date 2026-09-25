using Gateway.API.Api;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Notification.Contract.Models;

namespace Gateway.API.Controllers;

[ApiController]
[Route("api/notifications")]
[AuthFilter]
public sealed class NotificationsController(
    ILogger<BaseApiController> logger,
    NotificationApiClient client) : GatewayApiController(logger)
{
    [HttpGet]
    public Task<IActionResult> List(string? cursor = null, int limit = 50, bool unreadOnly = false,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            () => client.GetNotificationsAsync(cursor, limit, unreadOnly, cancellationToken),
            response => Ok(response), cancellationToken, "Notification history request");

    [HttpGet("unread-count")]
    public Task<IActionResult> UnreadCount(CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => client.GetUnreadCountAsync(cancellationToken),
            response => Ok(response), cancellationToken, "Notification unread count request");

    [HttpPut("{id:guid}/read")]
    public Task<IActionResult> Read(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => client.MarkReadAsync(id, cancellationToken),
            () => NoContent(), cancellationToken, "Mark notification read request");

    [HttpPut("read-all")]
    public Task<IActionResult> ReadAll(MarkAllNotificationsReadRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => client.MarkAllReadAsync(request, cancellationToken),
            response => Ok(response), cancellationToken, "Mark all notifications read request");

    [HttpGet("/api/notification-preferences")]
    public Task<IActionResult> Preferences(CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => client.GetPreferencesAsync(cancellationToken),
            response => Ok(response), cancellationToken, "Notification preferences request");

    [HttpPut("/api/notification-preferences")]
    public Task<IActionResult> UpdatePreferences(UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(() => client.UpdatePreferencesAsync(request, cancellationToken),
            () => NoContent(), cancellationToken, "Update notification preferences request");
}
