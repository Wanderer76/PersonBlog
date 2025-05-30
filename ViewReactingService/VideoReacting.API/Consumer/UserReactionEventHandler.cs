//using MessageBus.EventHandler;
//using MessageBus.Shared.Configs;
//using MessageBus.Shared.Events;
//using MessageBus;
//using Shared.Persistence;
//using Shared.Services;
//using System.Text.Json;
//using ViewReacting.Domain.Entities;
//using Microsoft.EntityFrameworkCore;

//namespace VideoReacting.API.Consumer
//{
//    public class UserReactionEventHandler : IEventHandler<UserReactionPostEvent>
//    {
//        private readonly IReadWriteRepository<IVideoReactEntity> videoViewRepository;

//        public UserReactionEventHandler(IReadWriteRepository<IVideoReactEntity> videoViewRepository)
//        {
//            this.videoViewRepository = videoViewRepository;
//        }

//        public async Task Handle(MessageContext<UserReactionPostEvent> @event)
//        {
//            var userId = @event.Message.UserId;
//            var ipAddress = @event.Message.RemoteIp;

//            var videoEvent = await videoViewRepository.Get<ReactingEvent>()
//                .FirstAsync(x => x.Id == @event.Message.EventId);

//            var hasView = await videoViewRepository.Get<PostReaction>()
//                .Where(x => x.UserId == userId || x.IpAddress == ipAddress)
//                .FirstOrDefaultAsync();

//            if (hasView == null && @event.Message.IsViewed)
//            {
//                hasView = new PostReaction(userId, ipAddress, @event.Message.PostId, @event.Message.ReactionTime, @event.Message.IsLike);
//                videoViewRepository.Add(hasView);
//            }
//            else
//            {
//                videoViewRepository.Attach(hasView);
//                hasView.IsLike = @event.Message.IsLike;
//            }

//            @event.Message.EventId = GuidService.GetNewGuid();
//            @event.Message.IsLike = hasView.IsLike;

//            var syncEvent = new ReactingEvent
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
