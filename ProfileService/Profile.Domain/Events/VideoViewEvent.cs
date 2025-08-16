using Blog.Contracts.Events;
using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Models;
using MessageBus.Shared.Configs;
using Profile.Domain.Services;
using Shared.Services;
using System.Text.Json;
using Profile.Domain.Entities;

namespace Profile.Domain.Events;


public struct QueueConstants
{
    public const string Exchange = "view-reacting";
    public const string QueueName = "video-reacting";
    public const string RoutingKey = "set-view";
}

[EventPublish(Exchange = QueueConstants.Exchange, RoutingKey = QueueConstants.RoutingKey)]
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
    private readonly ICacheService _cacheService;

    public VideoViewEventHandler(IViewHistoryService viewHistoryService, ICacheService cacheService)
    {
        _viewHistoryService = viewHistoryService;
        _cacheService = cacheService;
    }

    public async Task Handle(IMessageContext<VideoViewEvent> @event)
    {
        var result = await _viewHistoryService.CreateOrUpdateViewHistory(new UserPostView(
             @event.Message.UserId,
             @event.Message.PostId,
             @event.Message.WatchedTime,
             @event.Message.IsCompleteWatch
             ));
        await _cacheService.RemoveCachedDataAsync(new UserPostViewCacheKey(@event.Message.UserId));
        {
            await @event.PublishAsync(BaseEvent<UserViewedSyncEvent>.Create(
                new UserViewedSyncEvent
                {
                    EventId = @event.Message.UserId,
                    IsViewed = result.Value == UpdateViewState.Created ? @event.Message.IsCompleteWatch : true,
                    PostId = @event.Message.PostId,
                    UserId = @event.Message.UserId,
                    WatchedTime = DateTimeService.Now(),
                }
            ));
        }
    }
}
