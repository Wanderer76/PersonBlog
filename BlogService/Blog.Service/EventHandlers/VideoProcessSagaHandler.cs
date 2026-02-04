using Blog.Domain.Entities;
using Blog.Domain.Events;
using MessageBus;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.EventHandlers;

public sealed class VideoProcessSagaHandler :
    IEventHandler<CombineFileChunksCommand>,
    IEventHandler<ChunksCombinedResponse>,
    IEventHandler<VideoConvertedResponse>,
    IEventHandler<VideoPublishedResponse>
{
    private readonly IReadWriteRepository<IBlogEntity> _repository;
    private readonly IServiceProvider _serviceProvider;

    public VideoProcessSagaHandler(IReadWriteRepository<IBlogEntity> repository, IServiceProvider serviceProvider)
    {
        _repository = repository;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(IMessageContext<CombineFileChunksCommand> @event)
    {
        var saga = await _repository.Get<VideoProcessingSagaState>()
            .Where(x => x.CorrelationId == @event.Message.VideoMetadataId)
            .FirstOrDefaultAsync();

        if (saga != null)
        {
            return;
        }
        saga = new VideoProcessingSagaState
        {
            CorrelationId = @event.Message.VideoMetadataId,
            CurrentState = nameof(CombineFileChunksCommand)
        };
        _repository.Add(saga);
        saga.VideoMetadataId = @event.Message.VideoMetadataId;
        saga.PostId = @event.Message.PostId;
        await @event.PublishAsync("video-event", "chunks.combine", @event.Message, new MessageProperty { CorrelationId = saga.CorrelationId.ToString() });


        await _repository.SaveChangesAsync();
    }

    public async Task Handle(IMessageContext<ChunksCombinedResponse> @event)
    {
        var saga = await _repository.Get<VideoProcessingSagaState>()
            .Where(x => x.CorrelationId == @event.Message.VideoMetadataId)
            .FirstOrDefaultAsync();
        if (saga == null)
        {
            return;
        }
        _repository.Attach(saga);
        await ProcessCombine(saga, @event);
        await _repository.SaveChangesAsync();
    }

    public async Task Handle(IMessageContext<VideoConvertedResponse> @event)
    {
        //var saga = await _repository.Get<VideoProcessingSagaState>()
        //    .Where(x => x.CorrelationId == @event.Message.VideoMetadataId)
        //    .FirstOrDefaultAsync();
        //if (saga == null)
        //{
        //    return;
        //}
        //_repository.Attach(saga);

        var message = @event.Message;
        var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredKeyedService<IEventHandler<VideoReadyToPublishEvent>>(typeof(VideoReadyToPublishEvent).Name);

        if (message.PreviewId != null)
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

        await _repository.SaveChangesAsync();

        await service.Handle(MessageContext.Create(@event.CorrelationId, new VideoReadyToPublishEvent
        {
            PostId = message.PostId,
            VideoMetadataId = message.VideoMetadataId,
            Duration = message.Duration,
            ObjectName = message.ObjectName!,
            PreviewId = message.PreviewId?.Id,
            ProcessState = message.ProcessState,
            CreatedAt = DateTimeService.Now()
        }, @event));

    }

    public async Task Handle(IMessageContext<VideoPublishedResponse> @event)
    {
        var saga = await _repository.Get<VideoProcessingSagaState>()
            .Where(x => x.CorrelationId == @event.Message.VideoMetadataId)
            .FirstOrDefaultAsync();
        if (saga == null)
        {
            return;
        }
        _repository.Attach(saga);
        ProcessFinal(saga, @event.Message);
    }

    private async Task StartSaga(VideoProcessingSagaState saga, CombineFileChunksCommand message)
    {

    }

    private void ProcessFinal(VideoProcessingSagaState saga, VideoPublishedResponse message)
    {
        throw new NotImplementedException();
    }



    private async Task ProcessCombine(VideoProcessingSagaState saga, IMessageContext<ChunksCombinedResponse> @event)
    {
        var message = @event.Message;
        saga.ObjectName = message.ObjectName;

        var video = await _repository.Get<VideoFile>()
        .Where(x => x.Id == message.VideoMetadataId)
        .FirstAsync();
        video.ObjectName = message.ObjectName;

        var hasPreviewId = await _repository.Get<Post>()
        .Where(x => x.Id == message.PostId)
        .Select(x => new { x.VideoPostInfo.PreviewId, x.BlogId })
        .FirstAsync();

        await @event.PublishAsync("video-event", "video.convert", new ConvertVideoCommand
        {
            VideoMetadataId = saga.VideoMetadataId,
            BlogId = hasPreviewId.BlogId,
            ObjectName = saga.ObjectName!,
            PostId = saga.PostId,
            VideoMetadata = video,
            HasPreviewId = hasPreviewId.PreviewId.HasValue
        }, new MessageProperty { CorrelationId = saga.CorrelationId.ToString() });
    }
}
