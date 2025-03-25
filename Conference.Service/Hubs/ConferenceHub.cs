using Conference.Domain.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Shared.Utils;

namespace Conference.Service.Hubs
{
    public class ConferenceHub : Hub<IConferenceHub>
    {
        private readonly IConferenceRoomService _conferenceRoomService;
        private readonly ICacheService _cacheService;


        public async Task CloseConnectionAsync(Guid roomId)
        {
            var session = TryGetSession();
            session.AssertFound();
            await _conferenceRoomService.RemoveParticipantToConferenceAsync(roomId, session.Value);
        }

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            Context.GetHttpContext()!.Request.Query.TryGetValue("id", out var value);
            var conferenceId = Guid.Parse(value.First()!);
            var sessionId = TryGetSession();
            var key = new ConferenceChatModelCacheKey(conferenceId);
            var model = await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key);
            if (model != null)
            {

            }
            else
            {
                model = new ConferenceChatModel
                {
                    ConferenceId = conferenceId,
                    ConferenceParticipants = new Dictionary<string, Guid>
                    {
                        {connectionId,sessionId!.Value}
                    }
                };
                await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromMinutes(50));
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        private Guid? TryGetSession()
        {
            var context = Context.GetHttpContext();
            var session = context.Request.Cookies["sessionId"];
            return session == null ? null : Guid.Parse(session);
        }
    }
    public class ConferenceChatModel
    {
        public Guid ConferenceId { get; set; }
        public Dictionary<string, Guid> ConferenceParticipants { get; set; }
    }
    public readonly struct ConferenceChatModelCacheKey
    {
        public const string Key = "HubConference";
        private readonly Guid _id;

        public ConferenceChatModelCacheKey(Guid id)
        {
            _id = id;
        }
        public static implicit operator string(ConferenceChatModelCacheKey key) => $"{Key}:{key._id}";
    }
}
