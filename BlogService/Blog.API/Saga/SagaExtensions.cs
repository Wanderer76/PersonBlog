using Blog.Contracts.Events;
using Blog.Service.EventHandlers;
using MessageBus;
using MessageBus.Models;

namespace Blog.API.Saga;

public static class SagaExtensions
{
    public static IMessageBusBuilder AddVideoConvertSaga(this IMessageBusBuilder builder)
    {
        builder.Services.AddScoped<VideoReadyToPublishEventHandler>();

        return builder
            .AddSubscription<UserViewedSyncEvent, SyncProfileViewsHandler>(x =>
            {
                x.QueueName = "video-sync";
                x.Exchange = new ExchangeParam
                {
                    Name = "view-reacting",
                    RoutingKey = "video.sync"
                };
            })
            .AddSubscription<UserReactionSyncEvent, SyncProfileViewsHandler>(x =>
            {
                x.QueueName = "video-sync";
                x.Exchange = new ExchangeParam
                {
                    Name = "view-reacting",
                    RoutingKey = "video.sync"
                };
            })
            .AddSubscription<VideoConvertedResponse, VideoProcessSagaHandler>(x =>
            {
                x.QueueName = "saga-queue-video";
                x.Exchange = new ExchangeParam
                {
                    Name = "video-event",
                    RoutingKey = "saga.video.convert"
                };
            });
    }
}
