using MessageBus.EventHandler;
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

    public VideoViewEventHandler(IViewHistoryService viewHistoryService)
    {
        _viewHistoryService = viewHistoryService;
    }

    public async Task Handle(VideoViewEvent @event)
    {
        await _viewHistoryService.CreateOrUpdateViewHistory(new UserPostView(
            @event.UserId,
            @event.PostId,
            @event.WatchedTime,
            @event.IsCompleteWatch
            ));
    }
}
