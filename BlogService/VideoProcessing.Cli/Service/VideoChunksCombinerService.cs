using Blog.Domain.Events;
using FileStorage.Service.Service;
using MassTransit;
using MessageBus;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace VideoProcessing.Cli.Service
{
    public class VideoChunksCombinerService : IEventHandler<CombineFileChunksCommand>, IConsumer<CombineFileChunksCommand>
    {
        private readonly IFileStorage storage;
        private readonly RabbitMqMessageBus _messageBus;
        private readonly IChannel _channel;
        private readonly IConnection _connection;
        public VideoChunksCombinerService(IFileStorageFactory storage, RabbitMqMessageBus messageBus)
        {
            this.storage = storage.CreateFileStorage();
            _connection = messageBus.GetConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _messageBus = messageBus;
        }

        public async Task Consume(ConsumeContext<CombineFileChunksCommand> context)
        {
            var cmd = context.Message;
            var response = await CombineChunks(cmd);
            await context.Publish(response);
        }

        public async Task Handle(MessageContext<CombineFileChunksCommand> @event)
        {
            var response = await CombineChunks(@event.Message);
            await _messageBus.PublishAsync(_channel, "video-event", "saga", response, new BasicProperties
            {
                CorrelationId = response.VideoMetadataId.ToString(),
            });
        }

        private async Task<ChunksCombinedResponse> CombineChunks(CombineFileChunksCommand @event)
        {
            try
            {
                var chunks = new List<(long Number, int Size, string ObjectName)>();

                await foreach (var chunk in storage.GetAllBucketObjects(@event.PostId, new VideoChunkUploadingInfo { FileId = @event.VideoMetadataId })
                    .Where(x => x.Headers != null && x.Headers.Count > 0))
                {
                    chunks.Add((long.Parse(chunk.Headers["ChunkNumber"]), int.Parse(chunk.Headers["ChunkSize"]), chunk.Objectname));
                }
                if (chunks.Count == 0)
                {
                    throw new ArgumentException("Не удалось собрать файл");
                }

                var memoryStream = new MemoryStream(chunks.Sum(x => x.Size));

                foreach (var chunk in chunks.OrderBy(x => x.Number))
                {
                    await storage.ReadFileAsync(@event.PostId, chunk.ObjectName, memoryStream);
                }
                memoryStream.Position = 0;
                var objectName = await storage.PutFileInBucketAsync(@event.PostId, @event.VideoMetadataId, memoryStream);


                var response = new ChunksCombinedResponse
                {
                    ObjectName = objectName,
                    VideoMetadataId = @event.VideoMetadataId,
                    PostId = @event.PostId,
                };

                foreach (var chunk in chunks)
                {
                    await storage.RemoveFileAsync(@event.PostId, chunk.ObjectName);
                }
                return response;
            }
            catch (Exception e)
            {
                return new ChunksCombinedResponse
                {
                    VideoMetadataId = @event.VideoMetadataId,
                    ErrorMessage = "Не обработать файл",
                    PostId = @event.PostId
                };
            }
        }
    }
}
