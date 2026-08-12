using Blog.Contracts.Events;
using Blog.Contracts.Models.Post;
using Blog.Domain.Entities;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.Service.EventHandlers;

public sealed class VideoReadyToPublishEventHandler : IEventHandler<VideoReadyToPublishEvent>
{
    private readonly IReadWriteRepository<IBlogEntity> _repository;
    private readonly ICacheService _cacheService;
    public VideoReadyToPublishEventHandler(IReadWriteRepository<IBlogEntity> repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task Handle(IMessageContext<VideoReadyToPublishEvent> @event)
    {
        try
        {
            await PrepareToPublish(@event.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A new upload replaces the previous VideoFile. A late conversion
            // response for that removed file is stale and must not fail the bus.
            var metadataStillExists = await _repository.Get<VideoFile>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == @event.Message.VideoMetadataId);

            if (metadataStillExists)
                throw;
        }
    }

    private async Task PrepareToPublish(VideoReadyToPublishEvent @event)
    {
        var fileMetadata = await _repository.Get<VideoFile>()
            .FirstOrDefaultAsync(x => x.Id == @event.VideoMetadataId);

        var post = await _repository.Get<Post>()
            .Include(x=>x.VideoPostInfo)
            .FirstOrDefaultAsync(x => x.Id == @event.PostId);

        if (fileMetadata == null || post == null)
            return;

        _repository.Attach(fileMetadata);
        _repository.Attach(post);

        if (@event.Error != null)
        {
            fileMetadata.ErrorMessage = @event.Error;
            post.ProcessState = ProcessState.Error;
        }
        else
        {
            if (@event.PreviewId != null)
            {
                post.VideoPostInfo.PreviewId = @event.PreviewId;
            }
            fileMetadata.ObjectName = @event.ObjectName;
            fileMetadata.Duration = @event.Duration;
            post.ProcessState = ProcessState.Complete;
            post.VideoPostInfo.VideoFileId = fileMetadata.Id;
        }
        var postUpdateEvent = new PostUpdateEvent
        {
            BlogId = post.BlogId,
            PostId = post.Id,
            ViewCount = post.ViewCount,
            CreatedAt = post.CreatedAt,
            Description = post.VideoPostInfo.Description,
            Title = post.Title,
            UpdateType = UpdateType.Create
        };

        _repository.Add(VideoProcessEvent.Create(postUpdateEvent));
        await _repository.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(new PostModelCacheKey(post.Id));
        await _cacheService.RemoveCachedDataAsync(new VideoMetadataCacheKey(post.Id));
    }
}
