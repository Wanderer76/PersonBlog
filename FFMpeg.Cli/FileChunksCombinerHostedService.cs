using FileStorage.Service.Models;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace FFMpeg.Cli
{
    public class FileChunksCombinerHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly IFileStorageFactory _fileStorageFactory;

        public FileChunksCombinerHostedService(IServiceScopeFactory serviceScope, IFileStorageFactory fileStorageFactory)
        {
            _serviceScope = serviceScope;
            _fileStorageFactory = fileStorageFactory;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //_timer = new Timer(StartTask, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            StartTask(null);
        }
        private void StartTask(object? state)
        {
            _ = ProcessCombineVideoEvents();
        }

        private async Task ProcessCombineVideoEvents()
        {
            while (true)
            {
                try
                {
                    using var scope = _serviceScope.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                    var events = await context.Get<CombineFileChunksEvent>()
                        .Where(x => x.IsCompleted == false)
                        .Take(10)
                        .ToListAsync();

                    if (events.Count != 0)
                    {
                        var storage = _fileStorageFactory.CreateFileStorage();

                        foreach (var @event in events)
                        {
                            await ProcessChunks(storage, @event);
                        }

                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task ProcessChunks(IFileStorage storage, CombineFileChunksEvent @event)
        {
            var scope = _serviceScope.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

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

            await foreach (var i in storage.GetAllBucketObjects(post.Id, new VideoChunkUploadingInfo
            {
                FileId = fileId
            }))
            {
                chunks.Add((long.Parse(i.Headers["ChunkNumber"]), int.Parse(i.Headers["ChunkSize"]), i.Objectname));
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
                IsCompleted = false,
                UserProfileId = profileId,
                ObjectName = objectName,
                FileId = videoMetadata.Id
            };
            context.Add(videoCreateEvent);
            context.Attach(@event);
            @event.IsCompleted = true;
            await context.SaveChangesAsync();
            foreach (var chunk in chunks)
            {
                await storage.RemoveFileAsync(post.Id, chunk.ObjectName);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
