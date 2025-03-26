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

        public ConferenceHub(IConferenceRoomService conferenceRoomService, ICacheService cacheService)
        {
            _conferenceRoomService = conferenceRoomService;
            _cacheService = cacheService;
        }

        public async Task CloseConnectionAsync(Guid roomId)
        {
            var session = TryGetSession();
            session.AssertFound();
            await _conferenceRoomService.RemoveParticipantToConferenceAsync(roomId, session.Value);
        }

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var httpContext = Context.GetHttpContext();
            httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
            var conferenceId = Guid.Parse(value.First()!);
            var sessionId = TryGetSession();

            if (sessionId == null)
                return;

            var key = new ConferenceChatModelCacheKey(conferenceId);
            var model = await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key);
            if (model != null)
            {
                await _conferenceRoomService.AddParticipantToConferenceAsync(conferenceId, sessionId!.Value);
                if (!model.ConferenceParticipants.Any(x => x.Key == sessionId!.Value))
                {
                    model.ConferenceParticipants.Add(sessionId!.Value, connectionId);
                    await Groups.AddToGroupAsync(connectionId, conferenceId.ToString());
                    await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromHours(24));
                }
            }
            else
            {
                model = new ConferenceChatModel
                {
                    ConferenceId = conferenceId,
                    ConferenceParticipants = new Dictionary<Guid, string>
                    {
                        {sessionId!.Value,connectionId}
                    }
                };
                await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromMinutes(50));
                await Groups.AddToGroupAsync(connectionId, conferenceId.ToString());
            }
            await Clients.Group(conferenceId.ToString()).OnConferenceConnect($"Присоединилось пользователей: {model.ConferenceParticipants.Count}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            //var connectionId = Context.ConnectionId;
            var httpContext = Context.GetHttpContext(); 
            httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
            var conferenceId = Guid.Parse(value.First()!);
            var sessionId = TryGetSession();

            if (sessionId == null)
            {
                return;
            }

            var key = new ConferenceChatModelCacheKey(conferenceId);
            var model = await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key);
            if (model != null)
            {
                if (model.ConferenceParticipants.TryGetValue(sessionId.Value, out var connection))
                {
                    await _conferenceRoomService.RemoveParticipantToConferenceAsync(conferenceId, sessionId.Value);
                    model.ConferenceParticipants.Remove(sessionId.Value);
                    await Groups.RemoveFromGroupAsync(connection, conferenceId.ToString());
                    await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromMinutes(50));
                }
                await Clients.Group(conferenceId.ToString()).OnConferenceConnect($"Присоединилось пользователей: {model.ConferenceParticipants.Count}");
            }
            await base.OnDisconnectedAsync(exception);
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
        public Dictionary<Guid, string> ConferenceParticipants { get; set; }
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
