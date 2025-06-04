using Blog.Domain.Entities;
using Blog.Domain.Events;
using MessageBus;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
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
        private readonly IServiceProvider _serviceProvider;

        public VideoProcessSagaHandler(IReadWriteRepository<IBlogEntity> repository, IServiceProvider serviceProvider)
        {
            _repository = repository;
            _serviceProvider = serviceProvider;
        }

        public async Task Handle(IMessageContext<CombineFileChunksCommand> @event)
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
            saga.VideoMetadataId = @event.Message.VideoMetadataId;
            saga.PostId = @event.Message.PostId;
            await @event.PublishAsync("video-event", "chunks.combine", @event.Message, new MessageProperty { CorrelationId = saga.CorrelationId.ToString() });


            await _repository.SaveChangesAsync();
        }

        public async Task Handle(IMessageContext<ChunksCombinedResponse> @event)
        {
            var saga = await _repository.Get<VideoProcessingSagaState>()
                .Where(x => x.CorrelationId == @event.CorrelationId)
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
            var saga = await _repository.Get<VideoProcessingSagaState>()
                .Where(x => x.CorrelationId == @event.CorrelationId)
                .FirstOrDefaultAsync();
            if (saga == null)
            {
                return;
            }
            _repository.Attach(saga);
            await ProcessConverted(saga, @event);
            await _repository.SaveChangesAsync();
        }

        public async Task Handle(IMessageContext<VideoPublishedResponse> @event)
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

        }

        private void ProcessFinal(VideoProcessingSagaState saga, VideoPublishedResponse message)
        {
            throw new NotImplementedException();
        }

        private async Task ProcessConverted(VideoProcessingSagaState saga, IMessageContext<VideoConvertedResponse> context)
        {
            var message = context.Message;
            saga.ObjectName = message.ObjectName;
            var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetKeyedService<IEventHandler<VideoReadyToPublishEvent>>(typeof(VideoReadyToPublishEvent).Name);
            await service.Handle(MessageContext.Create(saga.CorrelationId, new VideoReadyToPublishEvent
            {
                PostId = message.PostId,
                VideoMetadataId = message.VideoMetadataId,
                Duration = message.Duration,
                ObjectName = message.ObjectName!,
                PreviewId = message.PreviewId,
                ProcessState = message.ProcessState,
                CreatedAt = DateTimeService.Now()
            }, context));
            saga.CurrentState = nameof(VideoPublishedResponse);
        }

        private async Task ProcessCombine(VideoProcessingSagaState saga, IMessageContext<ChunksCombinedResponse> @event)
        {
            var message = @event.Message;
            saga.ObjectName = message.ObjectName;

            var video = await _repository.Get<VideoMetadata>()
            .Where(x => x.Id == message.VideoMetadataId)
            .FirstAsync();
            video.ObjectName = message.ObjectName;

            var hasPreviewId = await _repository.Get<Post>()
            .Where(x => x.Id == message.PostId)
            .Select(x => x.PreviewId)
            .FirstAsync();

            await @event.PublishAsync("video-event", "video.convert", new ConvertVideoCommand
            {
                VideoMetadataId = saga.VideoMetadataId,
                ObjectName = saga.ObjectName!,
                PostId = saga.PostId,
                VideoMetadata = video,
                HasPreviewId = !string.IsNullOrWhiteSpace(hasPreviewId)
            }, new MessageProperty { CorrelationId = saga.CorrelationId.ToString() });
        }
        //async Task IEventHandler.Handle(MessageContext @event)
        //{
        //    switch (@event.Message)
        //    {
        //        case CombineFileChunksCommand command:
        //            await Handle(MessageContext.Create(@event.CorrelationId, command));
        //            break;
        //        case ChunksCombinedResponse response:
        //            await Handle(MessageContext.Create(@event.CorrelationId, response));
        //            break;
        //        case VideoConvertedResponse response:
        //            await Handle(MessageContext.Create(@event.CorrelationId, response));
        //            break;
        //        case VideoPublishedResponse response:
        //            await Handle(MessageContext.Create(@event.CorrelationId, response));
        //            break;
        //        default:
        //            throw new ArgumentException($"Unsupported event type: {@event.Message.GetType()}");
        //    }
        //}
    }
}
