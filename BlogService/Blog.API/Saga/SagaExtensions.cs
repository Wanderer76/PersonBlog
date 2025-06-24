using Blog.API.Handlers;
using Blog.Contracts.Events;
using Blog.Domain.Events;
using MessageBus;
using MessageBus.Models;

namespace Blog.API.Saga
{
    public static class SagaExtensions
    {
        public static IMessageBusBuilder AddVideoConvertSaga(this IMessageBusBuilder builder)
        {
            return builder
                .AddSubscription<UserViewedSyncEvent, SyncProfileViewsHandler>(x =>
                {
                    x.Name = "video-sync";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "view-reacting",
                        RoutingKey = "video.sync"
                    };
                })
                .AddSubscription<UserReactionSyncEvent, SyncProfileViewsHandler>(x =>
                {
                    x.Name = "video-sync";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "view-reacting",
                        RoutingKey = "video.sync"
                    };
                })
                .AddSubscription<CombineFileChunksCommand, VideoProcessSagaHandler>(x =>
                {
                    x.Name = "saga-queue-chunks-command";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "video-event",
                        RoutingKey = "saga"
                    };
                })
                .AddSubscription<ChunksCombinedResponse, VideoProcessSagaHandler>(x =>
                {
                    x.Name = "saga-queue-chunks-response";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "video-event",
                        RoutingKey = "saga.chunks.response"
                    };
                })
                .AddSubscription<VideoConvertedResponse, VideoProcessSagaHandler>(x =>
                {
                    x.Name = "saga-queue-video";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "video-event",
                        RoutingKey = "saga.video.convert"
                    };
                })
                .AddSubscription<VideoPublishedResponse, VideoProcessSagaHandler>(x =>
                {
                    x.Name = "saga-queue-publish";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "video-event",
                        RoutingKey = "saga.publish"
                    };
                })
                .AddSubscription<VideoReadyToPublishEvent, VideoReadyToPublishEventHandler>(x =>
                {
                    x.Name = "saga-queue-publish";
                    x.Exchange = new ExchangeParam
                    {
                        Name = "video-event",
                        RoutingKey = "saga.publish"
                    };
                });
        }
    }
}
