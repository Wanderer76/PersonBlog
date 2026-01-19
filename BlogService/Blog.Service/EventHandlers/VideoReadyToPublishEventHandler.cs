using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Domain.Events;
using Blog.Service.Models.Post;
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
        var post = await PrepareToPublish(@event.Message);
    }

    private async Task<Post> PrepareToPublish(VideoReadyToPublishEvent @event)
    {
        var fileMetadata = await _repository.Get<VideoFile>()
                        .FirstAsync(x => x.Id == @event.VideoMetadataId);

        var post = await _repository.Get<Post>()
            .Include(x=>x.VideoPostInfo)
            .FirstAsync(x => x.Id == @event.PostId);

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
        return post;
    }
}
