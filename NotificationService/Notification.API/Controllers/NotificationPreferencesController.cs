using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Preferences;
using Notification.Contract.Models;
using Notification.Domain.Entities;
using Shared.Services;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notification-preferences")]
[AuthFilter]
public sealed class NotificationPreferencesController(
    GetNotificationPreferences getPreferences,
    UpdateNotificationPreferences updatePreferences,
    IDateTimeManager dateTimeManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NotificationPreferenceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceResponse>>> Get(CancellationToken cancellationToken = default)
    {
        var result = await getPreferences.ExecuteAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value.Select(Map).ToArray())
            : BadRequest(result.Errors);
    }

    [HttpPut]
    public async Task<IActionResult> Put(UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Preferences is null)
            return BadRequest(new[] { new Shared.Utils.Error("preferences", "Preferences are required.") });

        var preferences = new List<NotificationPreference>();
        foreach (var item in request.Preferences)
        {
            if (!Enum.TryParse<NotificationKind>(item.Kind, true, out var kind) ||
                !Enum.TryParse<DeliveryType>(item.Channel, true, out var channel))
                return BadRequest(new[] { new Shared.Utils.Error("preferences", "Unknown kind or channel.") });
            preferences.Add(new NotificationPreference(kind, channel, item.Enabled));
        }

        var result = await updatePreferences.ExecuteAsync(preferences, dateTimeManager.UtcNow(), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Errors);
    }

    private static NotificationPreferenceResponse Map(NotificationPreference preference) =>
        new(preference.Kind.ToString(), preference.Channel.ToString(), preference.Enabled);
}
