using FFmpeg.Service;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

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
            _timer = new Timer(StartTask, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void StartTask(object? state)
        {
            _ = ProcessUploadVideoEvents();
        }

        private async Task ProcessUploadVideoEvents()
        {
            using var scope = _serviceScope.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

            var events = await context.Get<VideoUploadEvent>()
                .Where(x => x.IsCompleted == false)
                .Take(10)
                .ToListAsync();

            if (events.Any())
            {
                var storage = _fileStorageFactory.CreateFileStorage();

                //events.AsParallel().ForAll(@event =>
                //{
                //    using var parallelScope = _serviceScope.CreateScope();
                //    var dbContext = parallelScope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();
                //    await ProcessFile(dbContext, storage, @event);
                //})


                foreach (var @event in events.AsParallel())
                {
                    await ProcessFile(storage, @event);
                }

            }
            else
            {
                await Task.Delay(1000);
            }
        }

        private async Task ProcessFile(IFileStorage storage, VideoUploadEvent @event)
        {
            using var parallelScope = _serviceScope.CreateScope();
            var context = parallelScope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

            var fileMetadata = await context.Get<VideoMetadata>()
                .FirstAsync(x => x.ObjectName == @event.ObjectName);

            var url = await storage.GetFileUrlAsync(@event.UserProfileId, @event.ObjectName);
            var fileId = GuidService.GetNewGuid();
            var fileName = Path.Combine(path, fileId.ToString() + fileMetadata.FileExtension);
            try
            {
                await FFMpegService.ConvertToHlsAsync(new Uri(url), fileName);
                using var fileStream = new FileStream(fileName, FileMode.Open);
                using var copyStream = new MemoryStream();
                await fileStream.CopyToAsync(copyStream);
                copyStream.Position = 0;
                var objectName = await storage.PutFileWithOriginalResolutionAsync(@event.UserProfileId, fileId, copyStream, FileStorage.Service.Models.VideoResolution.Hd);
                var newVideoMetadata = new VideoMetadata
                {
                    Id = fileId,
                    ContentType = fileMetadata.ContentType,
                    ObjectName = objectName,
                    CreatedAt = DateTime.UtcNow,
                    Length = copyStream.Length,
                    Name = @event.ObjectName,
                    FileExtension = Path.GetExtension(fileName),
                    PostId = fileMetadata.PostId
                };

                context.Add(newVideoMetadata);
                await context.SaveChangesAsync();
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
