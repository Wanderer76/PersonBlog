﻿//using Infrastructure.Models;
//using MessageBus;
//using MessageBus.EventHandler;
//using MessageBus.Shared.Configs;
//using MessageBus.Shared.Events;
//using Microsoft.EntityFrameworkCore;
//using Shared.Persistence;
//using Shared.Services;
//using System.Text.Json;
//using Video.Domain.Entities;
//using Video.Domain.Events;

//namespace ReactionProcessing.Cli.Handlers
//{
//    public class VideoViewEventHandler : IEventHandler<UserViewedPostEvent>
//    {
//        private readonly RabbitMqMessageBus _messageBus;
//        private readonly RabbitMqVideoReactionConfig _reactionConfig = new();
//        private readonly IReadWriteRepository<IVideoViewEntity> videoViewRepository;

//        public VideoViewEventHandler(RabbitMqMessageBus messageBus, IReadWriteRepository<IVideoViewEntity> videoViewRepository)
//        {
//            _messageBus = messageBus;
//            this.videoViewRepository = videoViewRepository;
//        }

//        public async Task Handle(MessageContext<UserViewedPostEvent> @event)
//        {
//            var userId = @event.Message.UserId;
//            var ipAddress = @event.Message.RemoteIp;

//            var videoEvent = await videoViewRepository.Get<VideoEvent>()
//                .FirstAsync(x => x.Id == @event.Message.EventId);

//            var hasView = await videoViewRepository.Get<PostViewer>()
//                .Where(x => x.UserId == userId || x.UserIpAddress == ipAddress)
//                .FirstOrDefaultAsync();

//            if (hasView == null && @event.Message.IsViewed)
//            {
//                hasView = new PostViewer
//                {
//                    Id = GuidService.GetNewGuid(),
//                    PostId = @event.Message.PostId,
//                    IsLike = @event.Message.IsLike,
//                    UserId = userId,
//                    UserIpAddress = ipAddress,
//                };
//                videoViewRepository.Add(hasView);
//            }
//            else
//            {
//                videoViewRepository.Attach(hasView);
//                hasView.UserIpAddress = ipAddress;
//                hasView.UserId = userId;
//                hasView.IsLike = @event.Message.IsLike;
//            }

//            @event.Message.EventId = GuidService.GetNewGuid();
//            @event.Message.IsLike = hasView.IsLike;

//            var syncEvent = new VideoEvent
//            {
//                Id = @event.Message.EventId,
//                EventData = JsonSerializer.Serialize(@event),
//                EventType = nameof(UserViewedSyncEvent),
//            };

//            videoViewRepository.Add(syncEvent);

//            await videoViewRepository.SaveChangesAsync();
//        }

//    }
//}

