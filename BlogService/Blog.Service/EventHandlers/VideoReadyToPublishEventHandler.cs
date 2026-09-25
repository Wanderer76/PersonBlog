using Blog.Contracts.Events;
using Blog.Contracts.Models.Post;
using Blog.Domain.Entities;
using Blog.Service.Events;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

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
            .Include(x => x.VideoPostInfo).ThenInclude(x => x.PostCategories)
            .Include(x => x.VideoPostInfo).ThenInclude(x => x.PreviewFile)
            .FirstOrDefaultAsync(x => x.Id == @event.PostId);

        if (fileMetadata == null || post == null)
            return;

        var alreadyProcessed = @event.Error != null
            ? post.ProcessState == ProcessState.Error && fileMetadata.ErrorMessage == @event.Error
            : post.ProcessState == ProcessState.Complete
              && post.VideoPostInfo.VideoFileId == fileMetadata.Id
              && fileMetadata.ObjectName == @event.ObjectName;

        if (!alreadyProcessed)
        {
            _repository.Attach(fileMetadata);
            _repository.Attach(post);
            var recipientUserId = await _repository.Get<PersonBlog>()
                .Where(blog => blog.Id == post.BlogId)
                .Select(blog => (Guid?)blog.UserId)
                .SingleOrDefaultAsync();
            var occurredAt = DateTimeService.Now().ToUniversalTime();

            if (@event.Error != null)
            {
                fileMetadata.ErrorMessage = @event.Error;
                post.ProcessState = ProcessState.Error;
                if (recipientUserId.HasValue)
                {
                    _repository.Add(VideoProcessEvent.Create(new VideoProcessingFailedV1
                    {
                        EventId = GuidService.GetNewGuid(),
                        OccurredAt = occurredAt,
                        PostId = post.Id,
                        BlogId = post.BlogId,
                        RecipientUserId = recipientUserId.Value,
                        VideoMetadataId = fileMetadata.Id,
                        ProcessingAttemptId = fileMetadata.Id,
                        ErrorCode = "video_processing_failed"
                    }));
                }
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
                post.VideoPostInfo.VideoFile = fileMetadata;
                post.MarkRecommendationChanged();

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
                _repository.Add(VideoProcessEvent.Create(PostCatalogChangedV2Factory.Create(post)));
                if (recipientUserId.HasValue)
                {
                    _repository.Add(VideoProcessEvent.Create(new VideoProcessingCompletedV1
                    {
                        EventId = GuidService.GetNewGuid(),
                        OccurredAt = occurredAt,
                        PostId = post.Id,
                        BlogId = post.BlogId,
                        RecipientUserId = recipientUserId.Value,
                        VideoMetadataId = fileMetadata.Id,
                        ProcessingAttemptId = fileMetadata.Id
                    }));
                    if (post.CanNotifyAudience)
                    {
                        var publication = PostPublishedV1Factory.TryCreate(post, recipientUserId.Value, occurredAt);
                        if (publication is not null)
                            _repository.Add(VideoProcessEvent.Create(publication));
                    }
                }
            }
        }

        // This also commits a preview queued by VideoProcessSagaHandler. Replayed
        // responses reach this save without producing duplicate outbox messages.
        await _repository.SaveChangesAsync();
        await _cacheService.RemoveCachedDataAsync(new PostModelCacheKey(post.Id));
        await _cacheService.RemoveCachedDataAsync(new VideoMetadataCacheKey(post.Id));
    }
}
