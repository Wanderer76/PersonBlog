using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Shared.Configs;
using MessageBus.Shared.Events;
using Shared.Services;
using System.Text.Json;
using ViewReacting.Domain.Entities;
using ViewReacting.Domain.Services;

namespace ViewReacting.Domain.Events;


public struct QueueConstants
{
    public const string Exchange = "view-reacting";
    public const string QueueName = "video-reacting";
    public const string RoutingKey = "set-view";
}


public class VideoViewEvent
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public double WatchedTime { get; set; }
    public bool IsCompleteWatch { get; set; }
}

public class VideoViewEventHandler : IEventHandler<VideoViewEvent>
{
    private readonly IViewHistoryService _viewHistoryService;
    private readonly RabbitMqMessageBus _messageBus;
    private readonly RabbitMqVideoReactionConfig _reactingSettings = new();
    private readonly ICacheService _cacheService;
    public VideoViewEventHandler(IViewHistoryService viewHistoryService, RabbitMqMessageBus messageBus, ICacheService cacheService)
    {
        _viewHistoryService = viewHistoryService;
        _messageBus = messageBus;
        _cacheService = cacheService;
    }

    public async Task Handle(MessageContext<VideoViewEvent> @event)
    {
        var result = await _viewHistoryService.CreateOrUpdateViewHistory(new UserPostView(
             @event.Message.UserId,
             @event.Message.PostId,
             @event.Message.WatchedTime,
             @event.Message.IsCompleteWatch
             ));
        await _cacheService.RemoveCachedDataAsync(new UserPostViewCacheKey(@event.Message.UserId));
        //if (result.Value == UpdateViewState.Created)
        {
            await _messageBus.SendMessageAsync(RabbitMqVideoReactionConfig.ExchangeName, RabbitMqVideoReactionConfig.SyncRoutingKey,new ReactingEvent
            {
                EventData = JsonSerializer.Serialize(new UserViewedSyncEvent
                {
                    EventId = @event.Message.UserId,
                    IsViewed = result.Value == UpdateViewState.Created? @event.Message.IsCompleteWatch : true,
                    PostId = @event.Message.PostId,
                    UserId = @event.Message.UserId,
                    ViewedAt = DateTimeService.Now()
                }),
                EventType = nameof(UserViewedSyncEvent),
            });
        }
    }
}
