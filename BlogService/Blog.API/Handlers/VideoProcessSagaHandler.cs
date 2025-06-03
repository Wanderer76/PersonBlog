using Blog.Domain.Entities;
using Blog.Domain.Events;
using MessageBus;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Shared.Persistence;
using Shared.Services;

namespace Blog.API.Handlers
{
    public class VideoProcessSagaHandler :
        IEventHandler<CombineFileChunksCommand>,
        IEventHandler<ChunksCombinedResponse>,
        IEventHandler<VideoConvertedResponse>,
        IEventHandler<VideoPublishedResponse>
    {
        private readonly IReadWriteRepository<IBlogEntity> _repository;
        private readonly IMessagePublish _messageBus;
        private readonly IServiceProvider _serviceProvider;

        public VideoProcessSagaHandler(IReadWriteRepository<IBlogEntity> repository, IMessagePublish messageBus, IServiceProvider serviceProvider)
        {
            _repository = repository;
            _messageBus = messageBus;
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(MessageContext<CombineFileChunksCommand> @event)
        {
            var saga = await _repository.Get<VideoProcessingSagaState>()
                .Where(x => x.CorrelationId == @event.CorrelationId)
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
            await StartSaga(saga, @event.Message);
            await _repository.SaveChangesAsync();
        }

        public async Task Handle(MessageContext<ChunksCombinedResponse> @event)
        {
            var saga = await _repository.Get<VideoProcessingSagaState>()
                .Where(x => x.CorrelationId == @event.CorrelationId)
                .FirstOrDefaultAsync();
            if (saga == null)
            {
                return;
            }
            _repository.Attach(saga);
            await ProcessCombine(saga, @event.Message);
            await _repository.SaveChangesAsync();
        }

        public async Task Handle(MessageContext<VideoConvertedResponse> @event)
        {
            var saga = await _repository.Get<VideoProcessingSagaState>()
                .Where(x => x.CorrelationId == @event.CorrelationId)
                .FirstOrDefaultAsync();
            if (saga == null)
            {
                return;
            }
            _repository.Attach(saga);
            await ProcessConverted(saga, @event.Message);
            await _repository.SaveChangesAsync();
        }

        public async Task Handle(MessageContext<VideoPublishedResponse> @event)
        {
            var saga = await _repository.Get<VideoProcessingSagaState>()
                .Where(x => x.CorrelationId == @event.CorrelationId)
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
            saga.VideoMetadataId = message.VideoMetadataId;
            saga.PostId = message.PostId;

            await _messageBus.PublishAsync("video-event", "chunks.combine", message, new BasicProperties { CorrelationId = saga.CorrelationId.ToString() });
        }

        private void ProcessFinal(VideoProcessingSagaState saga, VideoPublishedResponse message)
        {
            throw new NotImplementedException();
        }

        private async Task ProcessConverted(VideoProcessingSagaState saga, VideoConvertedResponse message)
        {
            saga.ObjectName = message.ObjectName;
            var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetKeyedService<IEventHandler>(typeof(VideoReadyToPublishEvent).Name);
            await service.Handle(new MessageContext(saga.CorrelationId, new VideoReadyToPublishEvent
            {
                PostId = message.PostId,
                VideoMetadataId = message.VideoMetadataId,
                Duration = message.Duration,
                ObjectName = message.ObjectName!,
                PreviewId = message.PreviewId,
                ProcessState = message.ProcessState,
                CreatedAt = DateTimeService.Now()
            }));
            saga.CurrentState = nameof(VideoPublishedResponse);
        }

        private async Task ProcessCombine(VideoProcessingSagaState saga, ChunksCombinedResponse message)
        {
            saga.ObjectName = message.ObjectName;

            var video = await _repository.Get<VideoMetadata>()
            .Where(x => x.Id == message.VideoMetadataId)
            .FirstAsync();
            video.ObjectName = message.ObjectName;

            var hasPreviewId = await _repository.Get<Post>()
            .Where(x => x.Id == message.PostId)
            .Select(x => x.PreviewId)
            .FirstAsync();

            await _messageBus.PublishAsync("video-event", "video.convert", new ConvertVideoCommand
            {
                VideoMetadataId = saga.VideoMetadataId,
                ObjectName = saga.ObjectName!,
                PostId = saga.PostId,
                VideoMetadata = video,
                HasPreviewId = !string.IsNullOrWhiteSpace(hasPreviewId)
            }, new BasicProperties { CorrelationId = saga.CorrelationId.ToString() });
        }
        async Task IEventHandler.Handle(MessageContext @event)
        {
            switch (@event.Message)
            {
                case CombineFileChunksCommand command:
                    await Handle(MessageContext.Create(@event.CorrelationId, command));
                    break;
                case ChunksCombinedResponse response:
                    await Handle(MessageContext.Create(@event.CorrelationId, response));
                    break;
                case VideoConvertedResponse response:
                    await Handle(MessageContext.Create(@event.CorrelationId, response));
                    break;
                case VideoPublishedResponse response:
                    await Handle(MessageContext.Create(@event.CorrelationId, response));
                    break;
                default:
                    throw new ArgumentException($"Unsupported event type: {@event.Message.GetType()}");
            }
        }
    }
}
