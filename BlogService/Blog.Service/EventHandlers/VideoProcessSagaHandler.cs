using Blog.Contracts.Events;
using Blog.Domain.Entities;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.EventHandlers;

public sealed class VideoProcessSagaHandler : IEventHandler<VideoConvertedResponse>
{
    private readonly IReadWriteRepository<IBlogEntity> _repository;
    private readonly VideoReadyToPublishEventHandler _readyToPublishHandler;

    public VideoProcessSagaHandler(
        IReadWriteRepository<IBlogEntity> repository,
        VideoReadyToPublishEventHandler readyToPublishHandler)
    {
        _repository = repository;
        _readyToPublishHandler = readyToPublishHandler;
    }

    public async Task Handle(IMessageContext<VideoConvertedResponse> @event)
    {
        var message = @event.Message;

        // A previous upload can finish after it has already been replaced. Ignore
        // that response before persisting its preview or changing the current post.
        var metadataStillExists = await _repository.Get<VideoFile>()
            .AnyAsync(x => x.Id == message.VideoMetadataId && x.PostId == message.PostId);
        if (!metadataStillExists)
            return;

        if (message.PreviewId != null)
        {
            var previewExists = await _repository.Get<PostFile>()
                .AnyAsync(x => x.Id == message.PreviewId.Id);
            if (!previewExists)
            {
                _repository.Add(new PostFile
                {
                    Id = message.PreviewId.Id,
                    ContentType = message.PreviewId.ContentType,
                    CreatedAt = message.PreviewId.CreatedAt,
                    FileExtension = message.PreviewId.FileExtension,
                    Length = message.PreviewId.Length,
                    Name = message.PreviewId.Name,
                    ObjectName = message.PreviewId.ObjectName,
                    PostId = message.PostId
                });
            }
        }

        // Both handlers share the consumer scope and therefore the same DbContext.
        // VideoReadyToPublishEventHandler performs the single atomic save.
        await _readyToPublishHandler.Handle(MessageContext.Create(@event.CorrelationId, new VideoReadyToPublishEvent
        {
            PostId = message.PostId,
            VideoMetadataId = message.VideoMetadataId,
            Duration = message.Duration,
            ObjectName = message.ObjectName!,
            PreviewId = message.PreviewId?.Id,
            ProcessState = message.ProcessState,
            Error = message.Error,
            CreatedAt = DateTimeService.Now()
        }, @event));
    }
}
