using Blog.Domain.Entities;
using Blog.Domain.Events;
using FileStorage.Service.Models;
using FileStorage.Service.Service;
using Infrastructure.Models;
using MessageBus.EventHandler;
using MessageBus.Models;
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
                .Include(x => x.Post)
                .FirstAsync();
            _context.Attach(videoMetadata);

            try
            {
                var post = videoMetadata.Post;

                var profileId = await _context.Get<PersonBlog>()
                    .Where(x => x.Id == post.BlogId)
                    .Select(x => x.UserId)
                    .FirstAsync();

                var chunks = new List<(long Number, int Size, string ObjectName)>();

                await foreach (var chunk in storage.GetAllBucketObjects(post.Id, new VideoChunkUploadingInfo { FileId = fileId }).Where(x => x.Headers != null && x.Headers.Count > 0))
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
                    await storage.ReadFileAsync(post.Id, chunk.ObjectName, memoryStream);
                }
                memoryStream.Position = 0;
                var objectName = await storage.PutFileWithResolutionAsync(post.Id, fileId, memoryStream);

                videoMetadata.Length = memoryStream.Length;
                videoMetadata.ObjectName = objectName;
                videoMetadata.Resolution = VideoResolution.Original;
                var fileUrl = await storage.GetFileUrlAsync(post.Id, objectName);
                var videoCreateEvent = new VideoConvertEvent
                {
                    EventId = GuidService.GetNewGuid(),
                    FileUrl = fileUrl,
                    UserProfileId = profileId,
                    ObjectName = objectName,
                    FileId = videoMetadata.Id
                };

                var videoEvent = new ProfileEventMessages
                {
                    Id = videoCreateEvent.EventId,
                    EventData = JsonSerializer.Serialize(videoCreateEvent),
                    EventType = nameof(VideoConvertEvent),
                    State = EventState.Pending,
                };
                _context.Add(videoEvent);

                foreach (var chunk in chunks)
                {
                    await storage.RemoveFileAsync(post.Id, chunk.ObjectName);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                videoMetadata.ProcessState = ProcessState.Error;
                videoMetadata.ErrorMessage = "Не обработать файл";
                await _context.SaveChangesAsync();
                throw;
            }
        }
    }
}
