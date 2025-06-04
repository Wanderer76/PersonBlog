using Blog.Contracts.Events;
using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Shared.Configs;
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
        //if (result.Value == UpdateViewState.Created)
        {
            await @event.PublishAsync(
                new UserViewedSyncEvent
                {
                    EventId = @event.Message.UserId,
                    IsViewed = result.Value == UpdateViewState.Created ? @event.Message.IsCompleteWatch : true,
                    PostId = @event.Message.PostId,
                    UserId = @event.Message.UserId,
                    WatchedTime = DateTimeService.Now(),
                }
            );
        }
    }
}
