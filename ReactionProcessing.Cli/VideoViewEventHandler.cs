using Infrastructure.Models;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;
using System.Web;
using Video.Domain.Entities;
using Video.Domain.Events;

namespace ReactionProcessing.Cli
{
    public class VideoViewEventHandler : IEventHandler<UserViewedPostEvent>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqVideoReactionConfig _reactionConfig;
        private readonly IReadWriteRepository<IVideoViewEntity> videoViewRepository;

        //private const string prof = "http://localhost:7892/profile/api/Profile/hasView";

        public VideoViewEventHandler(IHttpClientFactory httpClientFactory, RabbitMqMessageBus messageBus, RabbitMqVideoReactionConfig reactionConfig, IReadWriteRepository<IVideoViewEntity> videoViewRepository)
        {
            _httpClientFactory = httpClientFactory;
            _messageBus = messageBus;
            _reactionConfig = reactionConfig;
            this.videoViewRepository = videoViewRepository;
        }

        public async Task Handle(UserViewedPostEvent @event)
        {
            var userId = @event.UserId;
            var ipAddress = @event.RemoteIp;

            var videoEvent = await videoViewRepository.Get<VideoEvent>()
                .FirstAsync(x => x.Id == @event.EventId);

            var hasView = await videoViewRepository.Get<PostViewer>()
                .Where(x => x.UserId == userId || x.UserIpAddress == ipAddress)
                .FirstOrDefaultAsync();

            if (hasView == null)
            {
                hasView = new PostViewer
                {
                    Id = GuidService.GetNewGuid(),
                    PostId = @event.PostId,
                    IsLike = @event.IsLike,
                    UserId = userId,
                    UserIpAddress = ipAddress
                };
                videoViewRepository.Add(hasView);
            }
            else
            {
                videoViewRepository.Attach(hasView);
                hasView.UserIpAddress = ipAddress;
                hasView.UserId = userId;
                hasView.IsLike = @event.IsLike;
            }

            @event.EventId = GuidService.GetNewGuid();


            var syncEvent = new VideoEvent
            {
                Id = @event.EventId,
                EventData = JsonSerializer.Serialize(@event),
                EventType = nameof(UserViewedSyncEvent),
                State = EventState.Pending,
            };

            videoViewRepository.Add(syncEvent);

            await videoViewRepository.SaveChangesAsync();

            //await _messageBus.SendMessageAsync(channel, _reactionConfig.ExchangeName, _reactionConfig.SyncRoutingKey, syncEvent);

            //using var client = _httpClientFactory.CreateClient("ProfileClient");

            //var builder = new UriBuilder(prof);

            //var query = HttpUtility.ParseQueryString(builder.Query);
            //if (userId.HasValue)
            //    query["userId"] = userId.ToString();
            //if (ipAddress != null)
            //    query["ipAddress"] = ipAddress;

            //builder.Query = query.ToString();
            //using var message = new HttpRequestMessage
            //{
            //    Method = HttpMethod.Get,
            //    RequestUri = builder.Uri,

            //};
            //var response = await client.SendAsync(message);

            //if (response.StatusCode == System.Net.HttpStatusCode.OK)
            //{
            //    var stream = await response.Content.ReadAsStreamAsync();

            //    var hasView = JsonSerializer.Deserialize<bool>(stream);
            //    if (!hasView)
            //    {
            //        //await using var connection = await _messageBus.GetConnectionAsync();
            //        //await using var channel = await connection.CreateChannelAsync();
            //        //await _messageBus.SendMessageAsync(channel, _reactionConfig.ExchangeName, _reactionConfig.RoutingKey,);
            //    }
        }

    }
}

