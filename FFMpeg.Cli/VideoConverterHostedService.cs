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

            var url = await storage.GetFileUrlAsync(fileMetadata.PostId, @event.ObjectName);

            var videoSizes = new List<VideoSize>() { VideoSize.Hd1080, VideoSize.Hd720, VideoSize.Vga, VideoSize.Nhd };

            // foreach (var videoSize in videoSizes)
            {
                //var fileName = Path.Combine(path, fileId.ToString() + fileMetadata.FileExtension);
                var dir = Path.Combine(path, fileMetadata.Id.ToString());
                string[] resolutions = { "1920x1080", "1280x720", "854x480", "640x360", "256x144" };
                string[] bitrates = { "5M", "3M", "1500k", "1000k", "500k" };
                string[] audioBitrates = { "96k", "96k", "64k", "48k", "48k" };
                var fileId = GuidService.GetNewGuid();
                try
                {
                    Directory.CreateDirectory(dir);
                    await FFMpegService.CreateHls(new Uri(url).AbsoluteUri, dir, resolutions, bitrates, audioBitrates, fileId.ToString(), fileMetadata.Id.ToString());
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        using var fileStream = new FileStream(file, FileMode.Open);
                        using var copyStream = new MemoryStream();
                        await fileStream.CopyToAsync(copyStream);
                        copyStream.Position = 0;
                        var objectName = await storage.PutFileWithResolutionAsync(fileMetadata.PostId, Path.GetFileName(file), copyStream);
                    }
                    foreach (string folder in Directory.EnumerateDirectories(dir))
                    {
                        foreach (var file in Directory.EnumerateFiles(folder))
                        {
                            using var fileStream = new FileStream(file, FileMode.Open);
                            using var copyStream = new MemoryStream();
                            await fileStream.CopyToAsync(copyStream);
                            copyStream.Position = 0;
                            var objectName = await storage.PutFileWithResolutionAsync(fileMetadata.PostId, GetRelativePath(file).Replace(Path.DirectorySeparatorChar, '/'), copyStream);
                        }
                    }


                    //var newVideoMetadata = new VideoMetadata
                    //{
                    //    Id = fileId,
                    //    ContentType = fileMetadata.ContentType,//"application/x-mpegURL",//
                    //    ObjectName = "master.m3u8",
                    //    CreatedAt = DateTime.UtcNow,
                    //    //Length = copyStream.Length,
                    //    Name = @event.ObjectName,
                    //    FileExtension = fileMetadata.FileExtension,
                    //    PostId = fileMetadata.PostId,
                    //    IsProcessed = false,
                    //  //  Resolution = videoSize.Convert(),
                    //};

                    //context.Add(newVideoMetadata);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
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
                    var objectName = await storage.PutFileAsync(fileMetadata.PostId, snapshotFileId, copyStream);

                    context.Attach(post);
                    post.PreviewId = objectName;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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

            context.Attach(fileMetadata);
            fileMetadata.IsProcessed = false;
            fileMetadata.ObjectName = $"{fileMetadata.Id}.m3u8";

            await context.SaveChangesAsync();
        }
        public static string GetRelativePath(string filePath)
        {
            // Используем Path.GetDirectoryName, чтобы получить родительскую директорию
            string directoryName = Path.GetDirectoryName(filePath);

            // Если родительская директория не null, то продолжаем
            if (directoryName != null)
            {
                // Разделяем путь на компоненты
                string[] pathComponents = directoryName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                // Если в пути есть хоть что-то кроме диска, получаем относительный путь
                if (pathComponents.Length > 1)
                {
                    string relativePath = string.Join(Path.DirectorySeparatorChar, pathComponents.Skip(pathComponents.Length - 1));
                    string fileName = Path.GetFileName(filePath);
                    return Path.Combine(relativePath, fileName);
                }
                else
                {
                    return Path.GetFileName(filePath);
                }
            }
            else
            {
                return Path.GetFileName(filePath);
            }
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
