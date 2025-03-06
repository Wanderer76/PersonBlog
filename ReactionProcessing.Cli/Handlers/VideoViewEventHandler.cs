using Infrastructure.Models;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;
using Video.Domain.Entities;
using Video.Domain.Events;

namespace ReactionProcessing.Cli.Handlers
{
    public class VideoViewEventHandler : IEventHandler<UserViewedPostEvent>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly RabbitMqVideoReactionConfig _reactionConfig = new();
        private readonly IReadWriteRepository<IVideoViewEntity> videoViewRepository;

        public VideoViewEventHandler(IHttpClientFactory httpClientFactory, RabbitMqMessageBus messageBus, IReadWriteRepository<IVideoViewEntity> videoViewRepository)
        {
            _httpClientFactory = httpClientFactory;
            _messageBus = messageBus;
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
                    UserIpAddress = ipAddress,
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
            @event.IsLike = hasView.IsLike;

            var syncEvent = new VideoEvent
            {
                Id = @event.EventId,
                EventData = JsonSerializer.Serialize(@event),
                EventType = nameof(UserViewedSyncEvent),
                State = EventState.Pending,
            };

            videoViewRepository.Add(syncEvent);

            await videoViewRepository.SaveChangesAsync();
        }

    }
}

