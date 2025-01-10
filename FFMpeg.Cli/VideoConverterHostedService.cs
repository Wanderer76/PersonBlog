using FFmpeg.Service;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;
using Xabe.FFmpeg;

namespace FFMpeg.Cli
{
    internal class VideoConverterHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly string path;
        private Timer _timer;

        public VideoConverterHostedService(IServiceScopeFactory serviceScope, IFileStorageFactory fileStorageFactory, IConfiguration configuration)
        {
            _serviceScope = serviceScope;
            _fileStorageFactory = fileStorageFactory;
            path = Path.GetFullPath(configuration["TempDir"]!);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //_timer = new Timer(StartTask, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            StartTask(null);
        }

        private void StartTask(object? state)
        {
            _ = ProcessUploadVideoEvents();
        }

        private async Task ProcessUploadVideoEvents()
        {
            while (true)
            {
                try
                {
                    using var scope = _serviceScope.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                    var events = await context.Get<VideoUploadEvent>()
                        .Where(x => x.IsCompleted == false)
                        .Take(10)
                        .ToListAsync();

                    if (events.Count != 0)
                    {
                        var storage = _fileStorageFactory.CreateFileStorage();

                        foreach (var @event in events)
                        {
                            await ProcessFile(storage, @event);
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

        private async Task ProcessFile(IFileStorage storage, VideoUploadEvent @event)
        {
            using var parallelScope = _serviceScope.CreateScope();
            var context = parallelScope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

            var fileMetadata = await context.Get<VideoMetadata>()
                .FirstAsync(x => x.ObjectName == @event.ObjectName);

            var url = await storage.GetFileUrlAsync(@event.UserProfileId, @event.ObjectName);

            var videoSizes = new List<VideoSize>() { VideoSize.Hd1080, VideoSize.Hd720, VideoSize.Vga, VideoSize.Nhd };

            foreach (var videoSize in videoSizes)
            {
                var fileId = GuidService.GetNewGuid();
                var fileName = Path.Combine(path, fileId.ToString() + fileMetadata.FileExtension);
                try
                {
                    await FFMpegService.ConvertToH264Async(new Uri(url).AbsoluteUri, fileName, videoSize);
                    using var fileStream = new FileStream(fileName, FileMode.Open);
                    using var copyStream = new MemoryStream();
                    await fileStream.CopyToAsync(copyStream);
                    copyStream.Position = 0;
                    var objectName = await storage.PutFileWithOriginalResolutionAsync(@event.UserProfileId, fileId, copyStream, videoSize.Convert());
                    var newVideoMetadata = new VideoMetadata
                    {
                        Id = fileId,
                        ContentType = fileMetadata.ContentType,
                        ObjectName = objectName,
                        CreatedAt = DateTime.UtcNow,
                        Length = copyStream.Length,
                        Name = @event.ObjectName,
                        FileExtension = Path.GetExtension(fileName),
                        PostId = fileMetadata.PostId,
                        Resolution = videoSize.Convert(),
                    };

                    context.Add(newVideoMetadata);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                }
            }

            var post = await context.Get<Post>()
                   .FirstAsync(x => x.Id == fileMetadata.PostId);

            if (post.Type == PostType.Video && string.IsNullOrWhiteSpace(post.PreviewId))
            {
                var snapshotFileId = GuidService.GetNewGuid();
                var snapshotFileName = Path.Combine(path, snapshotFileId.ToString() + ".png");

                try
                {
                    await FFMpegService.GeneratePreview(new Uri(url).AbsoluteUri, snapshotFileName);
                    using var fileStream = new FileStream(snapshotFileName, FileMode.Open);
                    using var copyStream = new MemoryStream();
                    await fileStream.CopyToAsync(copyStream);
                    copyStream.Position = 0;
                    var objectName = await storage.PutFileAsync(@event.UserProfileId, snapshotFileId, copyStream);

                    context.Attach(post);
                    post.PreviewId = objectName;
                }
                finally
                {
                    if (File.Exists(snapshotFileName))
                    {
                        File.Delete(snapshotFileName);
                    }
                }
            }

            context.Attach(@event);
            @event.IsCompleted = true;
            await context.SaveChangesAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
