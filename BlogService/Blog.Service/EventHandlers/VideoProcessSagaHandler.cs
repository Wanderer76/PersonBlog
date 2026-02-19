using Blog.Contracts.Events;
using Blog.Domain.Entities;
using MessageBus;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.EventHandlers;

public sealed class VideoProcessSagaHandler :
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

    private void ProcessFinal(VideoProcessingSagaState saga, VideoPublishedResponse message)
    {
        throw new NotImplementedException();
    }
}
