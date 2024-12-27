using FFmpeg.Service;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace FFMpeg.Cli
{
    internal class VideoConverterHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly IFileStorageFactory _fileStorageFactory;

        public VideoConverterHostedService(IServiceScopeFactory serviceScope, IFileStorageFactory fileStorageFactory)
        {
            _serviceScope = serviceScope;
            _fileStorageFactory = fileStorageFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceScope.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                var tempDir = scope.ServiceProvider.GetRequiredService<IConfiguration>()["TempDir"]!;
                var path = Path.GetFullPath(tempDir);

                var events = await context.Get<VideoUploadEvent>().ToListAsync();

                if (events.Any())
                {
                    var storage = _fileStorageFactory.CreateFileStorage();

                    foreach (var @event in events)
                    {
                        using var stream = new MemoryStream();
                        var fileMetadata = await context.Get<FileMetadata>()
                            .FirstAsync(x => x.ObjectName == @event.ObjectName);

                        var url = await storage.GetFileUrlAsync(@event.UserProfileId, @event.ObjectName);

                        var fileId = GuidService.GetNewGuid();

                        var fileName = Path.Combine(path, fileId.ToString());

                        await FFMpegService.ConvertToHlsAsync(new Uri(url), fileName);

                        using var fileStream = new FileStream(fileName, FileMode.Open);
                        await fileStream.CopyToAsync(stream);
                        stream.Position = 0;
                        var objectName = await storage.PutFileWithOriginalResolutionAsync(@event.UserProfileId, fileId, stream, FileStorage.Service.Models.VideoResolution.Hd);
                        var newVideoMetadata = new FileMetadata
                        {
                            Id = fileId,
                            ContentType = fileMetadata.ContentType,
                            ObjectName = objectName,
                            CreatedAt = DateTime.UtcNow,
                            Length = stream.Length,
                            Name = @event.ObjectName,
                            PostId = fileMetadata.PostId
                        };

                        context.Add(newVideoMetadata);
                        await context.SaveChangesAsync();
                    }
                }
                await Task.Delay(1000);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
