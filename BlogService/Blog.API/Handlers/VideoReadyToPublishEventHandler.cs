using Blog.Domain.Entities;
using Blog.Domain.Events;
using FFMpegCore;
using Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Security.AccessControl;

namespace Blog.API.Handlers
{
    public class VideoReadyToPublishEventHandler : IConsumer<VideoReadyToPublishEvent>
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
            var fileMetadata = await _repository.Get<VideoMetadata>()
                .FirstAsync(x => x.Id == @event.VideoMetadataId);

            var post = await _repository.Get<Post>()
                .FirstAsync(x=>x.Id== @event.PostId);

            _repository.Attach(fileMetadata);
            _repository.Attach(post);

            post.PreviewId = @event.PreviewId;
            fileMetadata.IsProcessed = false;
            fileMetadata.ObjectName = $"{fileMetadata.Id}.m3u8";
            fileMetadata.Duration = @event.Duration;
            fileMetadata.ProcessState = ProcessState.Complete;
            post.VideoFileId = fileMetadata.Id;

            await _repository.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync($"PostModel:{post.Id}");
        }
    }
}
