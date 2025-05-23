﻿using Infrastructure.Services;
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

    public async Task Handle(VideoViewEvent @event)
    {
        var result = await _viewHistoryService.CreateOrUpdateViewHistory(new UserPostView(
             @event.UserId,
             @event.PostId,
             @event.WatchedTime,
             @event.IsCompleteWatch
             ));
        await _cacheService.RemoveCachedDataAsync(new UserPostViewCacheKey(@event.UserId));
        if (result.Value == UpdateViewState.Created)
        {
            using var connection = await _messageBus.GetConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await _messageBus.SendMessageAsync(channel, _reactingSettings.ExchangeName, _reactingSettings.SyncRoutingKey,new ReactingEvent
            {
                EventData = JsonSerializer.Serialize(new UserViewedSyncEvent
                {
                    EventId = @event.UserId,
                    IsViewed = true,
                    PostId = @event.PostId,
                    UserId = @event.UserId,
                    ViewedAt = DateTimeService.Now()
                }),
                EventType = nameof(UserViewedSyncEvent),
            });
        }
    }
}
