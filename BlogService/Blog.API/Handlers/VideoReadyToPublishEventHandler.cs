﻿using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Domain.Events;
using Infrastructure.Services;
using MassTransit;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Text.Json;

namespace Blog.API.Handlers
{
    public class VideoReadyToPublishEventHandler : IConsumer<VideoReadyToPublishEvent>, IEventHandler<VideoReadyToPublishEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;
        private readonly ICacheService _cacheService;
        public VideoReadyToPublishEventHandler(IReadWriteRepository<IBlogEntity> repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task Consume(ConsumeContext<VideoReadyToPublishEvent> context)
        {
            var @event = context.Message;
            await PrepareToPublish(@event);
        }

        public async Task Handle(IMessageContext<VideoReadyToPublishEvent> @event)
        {
            var post = await PrepareToPublish(@event.Message);
        }

        private async Task<Post> PrepareToPublish(VideoReadyToPublishEvent @event)
        {
            var fileMetadata = await _repository.Get<VideoMetadata>()
                            .FirstAsync(x => x.Id == @event.VideoMetadataId);

            var post = await _repository.Get<Post>()
                .FirstAsync(x => x.Id == @event.PostId);

            _repository.Attach(fileMetadata);
            _repository.Attach(post);

            if (@event.Error != null)
            {
                fileMetadata.ErrorMessage = @event.Error;
                fileMetadata.ProcessState = ProcessState.Error;
                fileMetadata.IsProcessed = false;
            }
            else
            {
                post.PreviewId = @event.PreviewId;
                fileMetadata.IsProcessed = false;
                fileMetadata.ObjectName = $"{fileMetadata.Id}.m3u8";
                fileMetadata.Duration = @event.Duration;
                fileMetadata.ProcessState = ProcessState.Complete;
                post.VideoFileId = fileMetadata.Id;
            }
            var postUpdateEvent = new PostUpdateEvent
            {
                BlogId = post.BlogId,
                PostId = post.Id,
                ViewCount = post.ViewCount,
                CreatedAt = post.CreatedAt,
                Description = post.Description,
                Title = post.Title,
                UpdateType = UpdateType.Create
            };

            _repository.Add(new VideoProcessEvent { EventData = JsonSerializer.Serialize(postUpdateEvent), EventType = nameof(PostUpdateEvent) });
            await _repository.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync($"PostModel:{post.Id}");
            return post;
        }
    }
}
