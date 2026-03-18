using Blog.Contracts.Events;
using Infrastructure.Services;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;

namespace VideoProcessing.Cli.Service;

[Obsolete("Была добавлена multipart загрузка в s3, необходимость в этом сервисе отпала")]
public class VideoChunksCombinerService : IEventHandler<CombineFileChunksCommand>
{
    private readonly IFileStorage storage;
    public VideoChunksCombinerService(IFileStorageFactory storage)
    {
        this.storage = storage.CreateFileStorage();
    }

    public async Task Handle(IMessageContext<CombineFileChunksCommand> @event)
    {
        var response = await CombineChunks(@event.Message);
        await @event.PublishAsync("video-event", "saga.chunks.response", response);
    }

    private async Task<ChunksCombinedResponse> CombineChunks(CombineFileChunksCommand @event)
    {
        try
        {
            var chunks = new List<(long Number, int Size, string ObjectName)>();

            await foreach (var chunk in storage.GetAllBucketObjects(@event.PostId, new ChunkUploadingInfo(@event.VideoMetadataId, default))
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
            var objectName = await storage.PutFileAsync(@event.BlogId, $"{@event.PostId}/{@event.VideoMetadataId}", memoryStream);

            var response = new ChunksCombinedResponse
            {
                ObjectName = objectName,
                VideoMetadataId = @event.VideoMetadataId,
                PostId = @event.PostId,
            };

            //foreach (var chunk in chunks)
            //{
            //    await storage.RemoveFileAsync(@event.PostId, chunk.ObjectName);
            //}
            return response;
        }
        catch (Exception e)
        {
            return new ChunksCombinedResponse
            {
                VideoMetadataId = @event.VideoMetadataId,
                ErrorMessage = "Не удалось обработать файл",
                PostId = @event.PostId
            };
        }
    }
}
