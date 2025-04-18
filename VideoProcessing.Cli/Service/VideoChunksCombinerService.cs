using Blog.Domain.Entities;
using Blog.Domain.Events;
using FileStorage.Service.Models;
using FileStorage.Service.Service;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;

namespace VideoProcessing.Cli.Service
{
    public class VideoChunksCombinerService : IEventHandler<CombineFileChunksEvent>
    {
        private readonly IFileStorage storage;
        private readonly IReadWriteRepository<IProfileEntity> _context;

        public VideoChunksCombinerService(IFileStorageFactory storage, IReadWriteRepository<IProfileEntity> context)
        {
            this.storage = storage.CreateFileStorage();
            this._context = context;
        }
      
        public async Task Handle(CombineFileChunksEvent @event)
        {
            var fileId = @event.VideoMetadataId;

            var videoMetadata = await _context.Get<VideoMetadata>()
                .Where(x => x.Id == fileId)
                .FirstAsync();

            _context.Attach(videoMetadata);

            try
            {
                var chunks = new List<(long Number, int Size, string ObjectName)>();

                await foreach (var chunk in storage.GetAllBucketObjects(@event.PostId, new VideoChunkUploadingInfo { FileId = fileId })
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
                var objectName = await storage.PutFileInBucketAsync(@event.PostId, fileId, memoryStream);

                videoMetadata.ObjectName = objectName;

                var videoCreateEvent = new VideoConvertEvent
                {
                    EventId = GuidService.GetNewGuid(),
                    ObjectName = objectName,
                    FileId = videoMetadata.Id
                };

                var videoEvent = new VideoProcessEvent
                {
                    Id = videoCreateEvent.EventId,
                    EventData = JsonSerializer.Serialize(videoCreateEvent),
                    EventType = nameof(VideoConvertEvent),
                };
                _context.Add(videoEvent);

                foreach (var chunk in chunks)
                {
                    await storage.RemoveFileAsync(@event.PostId, chunk.ObjectName);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                videoMetadata.ProcessState = ProcessState.Error;
                videoMetadata.ErrorMessage = "Не обработать файл";
                await _context.SaveChangesAsync();
            }
        }
    }
}
