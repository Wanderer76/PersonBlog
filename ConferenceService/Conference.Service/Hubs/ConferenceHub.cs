using Conference.Domain.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Shared.Services;
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

            //if (!await _conferenceRoomService.IsConferenceActiveAsync(conferenceId))
            //{
            //    return;
            //}

            var key = new ConferenceChatModelCacheKey(conferenceId);
            var model = await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key);
            if (model != null)
            {
                await _conferenceRoomService.AddParticipantToConferenceAsync(conferenceId, sessionId!.Value);
                if (!model.ConferenceParticipants.Any(x => x.Key == sessionId!.Value))
                {
                    model.ConferenceParticipants.Add(sessionId!.Value, connectionId);
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
            }
            await Groups.AddToGroupAsync(connectionId, conferenceId.ToString());
            //await Clients.Group(conferenceId.ToString()).OnConferenceConnect($"Присоединилось пользователей: {model.ConferenceParticipants.Count}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var httpContext = Context.GetHttpContext();
            httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
            var conferenceId = value.First();
            //var sessionId = TryGetSession();

            //if (sessionId == null)
            //{
            //    return;
            //}
            await Groups.RemoveFromGroupAsync(connectionId, conferenceId);
            //var key = new ConferenceChatModelCacheKey(conferenceId);
            //var model = await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key);
            //if (model != null)
            //{
            //    if (model.ConferenceParticipants.TryGetValue(sessionId.Value, out var connection))
            //    {
            //        await _conferenceRoomService.RemoveParticipantToConferenceAsync(conferenceId, sessionId.Value);
            //        model.ConferenceParticipants.Remove(sessionId.Value);
            //        await Groups.RemoveFromGroupAsync(connection, conferenceId.ToString());
            //        await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromMinutes(50));
            //    }
            //    //await Clients.Group(conferenceId.ToString()).OnConferenceConnect($"Присоединилось пользователей: {model.ConferenceParticipants.Count}");
            //}
            await base.OnDisconnectedAsync(exception);
        }

        public async Task PauseVideo(double time)
        {
            var connectionId = Context.ConnectionId;
            var httpContext = Context.GetHttpContext();
            httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
            var conferenceId = value.First()!;
            await Clients.GroupExcept(conferenceId, [connectionId]).OnPause(time);
        }

        public async Task SetCurrentTime(double time)
        {
            var connectionId = Context.ConnectionId;
            var httpContext = Context.GetHttpContext();
            httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
            var conferenceId = Guid.Parse(value.First()!);
            var key = new ConferenceChatModelCacheKey(conferenceId);
            var model = (await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key))!;
            model.CurrentTime = Math.Max(model.CurrentTime, time);
            await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromMinutes(50));
        }

        public async Task ResumeVideo()
        {
            try
            {
                var connectionId = Context.ConnectionId;

                var httpContext = Context.GetHttpContext();
                httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
                var conferenceId = value.First()!;
                var key = new ConferenceChatModelCacheKey(Guid.Parse(conferenceId));
                var model = await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key);
                await Clients.GroupExcept(conferenceId, [connectionId]).OnPlay();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task Seek(double time)
        {
            var connectionId = Context.ConnectionId;
            var httpContext = Context.GetHttpContext();
            httpContext!.Request.Query.TryGetValue("conferenceId", out var value);
            var conferenceId = value.First()!;
            var key = new ConferenceChatModelCacheKey(Guid.Parse(conferenceId));
            var model = (await _cacheService.GetCachedDataAsync<ConferenceChatModel>(key))!;
            model.CurrentTime = time;
            await _cacheService.SetCachedDataAsync(key, model, TimeSpan.FromMinutes(50));
            await Clients.GroupExcept(conferenceId, [connectionId]).OnTimeSeek(time);
            //await Clients.Group(conferenceId).OnTimeSeek(time);
        }

        private Guid? TryGetSession()
        {
            var context = Context.GetHttpContext();
            var session = context.Request.Query.TryGetValue("token",out var value);
            return !session  ? null :JwtUtils.GetTokenRepresentaion(value).UserId;
        }
    }
    public class ConferenceChatModel
    {
        public Guid ConferenceId { get; set; }
        public double CurrentTime { get; set; }
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
