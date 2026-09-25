using Conference.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.Services;

namespace Conference.Service.Hubs;

[Authorize]
public class ConferenceHub(
    IConferenceRoomService conferenceRoomService,
    IConferenceStateStore stateStore) : Hub<IConferenceHub>
{
    public async Task CloseConnectionAsync(Guid roomId)
    {
        var conferenceId = GetConferenceId();
        if (roomId != conferenceId)
        {
            throw new HubException("The requested room does not match the current connection.");
        }

        var userId = GetUserId();
        await RemoveConnectionAsync(conferenceId, userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conferenceId.ToString());
        Context.Abort();
    }

    public override async Task OnConnectedAsync()
    {
        var conferenceId = GetConferenceId();
        var userId = GetUserId();

        if (!await conferenceRoomService.IsConferenceActiveAsync(conferenceId))
        {
            throw new HubException("Conference does not exist or is closed.");
        }

        await stateStore.AddConnectionAsync(conferenceId, userId, Context.ConnectionId);
        try
        {
            await conferenceRoomService.AddParticipantToConferenceAsync(conferenceId, userId, GetUserName());
            await Groups.AddToGroupAsync(Context.ConnectionId, conferenceId.ToString());
            await base.OnConnectedAsync();
        }
        catch
        {
            await stateStore.RemoveConnectionAsync(conferenceId, userId, Context.ConnectionId);
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var conferenceId = GetConferenceId();
            var userId = GetUserId();
            await RemoveConnectionAsync(conferenceId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conferenceId.ToString());
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public Task PauseVideo(double time)
    {
        ValidateTime(time);
        var conferenceId = GetConferenceId().ToString();
        return Clients.GroupExcept(conferenceId, [Context.ConnectionId]).OnPause(time);
    }

    public Task SetCurrentTime(double time)
    {
        return stateStore.SetCurrentTimeIfGreaterAsync(GetConferenceId(), time);
    }

    public Task ResumeVideo()
    {
        var conferenceId = GetConferenceId().ToString();
        return Clients.GroupExcept(conferenceId, [Context.ConnectionId]).OnPlay();
    }

    public async Task Seek(double time)
    {
        ValidateTime(time);
        var conferenceId = GetConferenceId();
        await stateStore.SetCurrentTimeAsync(conferenceId, time);
        await Clients.GroupExcept(conferenceId.ToString(), [Context.ConnectionId]).OnTimeSeek(time);
    }

    private async Task RemoveConnectionAsync(Guid conferenceId, Guid userId)
    {
        var isLastUserConnection = await stateStore.RemoveConnectionAsync(
            conferenceId,
            userId,
            Context.ConnectionId);

        if (isLastUserConnection)
        {
            await conferenceRoomService.RemoveParticipantToConferenceAsync(conferenceId, userId);
        }
    }

    private Guid GetConferenceId()
    {
        var value = Context.GetHttpContext()?.Request.Query["conferenceId"].FirstOrDefault();
        if (!Guid.TryParse(value, out var conferenceId))
        {
            throw new HubException("A valid conferenceId is required.");
        }

        return conferenceId;
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirst(AppClaimTypes.UserId)?.Value;
        if (!Guid.TryParse(value, out var userId) || userId == Guid.Empty)
        {
            throw new HubException("An authenticated user is required.");
        }

        return userId;
    }

    private string GetUserName()
    {
        return Context.User?.FindFirst(AppClaimTypes.Login)?.Value ?? "unknown";
    }

    private static void ValidateTime(double time)
    {
        if (!double.IsFinite(time) || time < 0)
        {
            throw new HubException("Video time must be a finite, non-negative value.");
        }
    }
}
