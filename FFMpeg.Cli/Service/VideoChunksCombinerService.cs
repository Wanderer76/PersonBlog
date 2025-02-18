using FileStorage.Service.Models;
using FileStorage.Service.Service;
using Infrastructure.Models;
using MessageBus.EventHandler;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Domain.Events;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;

namespace VideoProcessing.Cli.Service
{
    public class VideoChunksCombinerService : IEventHandler<CombineFileChunksEvent>
    {
        private readonly IFileStorage storage;
        private readonly IReadWriteRepository<IProfileEntity> context;

        public VideoChunksCombinerService(IFileStorageFactory storage, IReadWriteRepository<IProfileEntity> context)
        {
            this.storage = storage.CreateFileStorage();
            this.context = context;
        }

        public async Task Handle(CombineFileChunksEvent @event)
        {
            var fileId = @event.VideoMetadataId;

            var post = await context.Get<VideoMetadata>()
                .Where(x => x.Id == fileId)
                .Select(x => x.Post)
                .FirstAsync();

            var profileId = await context.Get<Blog>()
                .Where(x => x.Id == post.BlogId)
                .Select(x => x.ProfileId)
                .FirstAsync();

            var chunks = new List<(long Number, int Size, string ObjectName)>();

            await foreach (var i in storage.GetAllBucketObjects(post.Id, new VideoChunkUploadingInfo { FileId = fileId }).Where(x => x.Headers != null && x.Headers.Count > 0))
            {
                chunks.Add((long.Parse(i.Headers["ChunkNumber"]), int.Parse(i.Headers["ChunkSize"]), i.Objectname));
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

            var videoMetadata = await context.Get<VideoMetadata>()
               .Where(x => x.Id == fileId)
               .FirstAsync();

            context.Attach(videoMetadata);
            videoMetadata.Length = memoryStream.Length;
            videoMetadata.ObjectName = objectName;
            videoMetadata.Resolution = VideoResolution.Original;
            var fileUrl = await storage.GetFileUrlAsync(post.Id, objectName);
            var videoCreateEvent = new VideoUploadEvent
            {
                Id = GuidService.GetNewGuid(),
                FileUrl = fileUrl,
                UserProfileId = profileId,
                ObjectName = objectName,
                FileId = videoMetadata.Id
            };

            var videoEvent = new ProfileEventMessages
            {
                Id = GuidService.GetNewGuid(),
                EventData = JsonSerializer.Serialize(videoCreateEvent),
                EventType = nameof(VideoUploadEvent),
                State = EventState.Pending,
            };
            context.Add(videoEvent);

            foreach (var chunk in chunks)
            {
                await storage.RemoveFileAsync(post.Id, chunk.ObjectName);
            }
            await context.SaveChangesAsync();
        }

    }
}
