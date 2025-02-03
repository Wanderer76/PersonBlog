using FFmpeg.Service;
using FFMpeg.Cli.Models;
using FileStorage.Service.Service;
using Infrastructure.Models;
using MessageBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;
using Xabe.FFmpeg;

namespace FFMpeg.Cli
{
    internal class VideoConverterHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly IEnumerable<VideoPreset> _videoPresets;
        private readonly string _tempPath;
        private readonly IMessageBus _messageBus;

        public VideoConverterHostedService(IServiceScopeFactory serviceScope,
            IFileStorageFactory fileStorageFactory, IConfiguration configuration, IMessageBus messageBus)
        {
            _serviceScope = serviceScope;
            _fileStorageFactory = fileStorageFactory;
            _tempPath = Path.GetFullPath(configuration["TempDir"]!);
            _videoPresets = configuration.GetSection("VideoPresets").Get<List<VideoPreset>>()!;
            _messageBus = messageBus;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = ProcessUploadVideoEvents();
            return Task.CompletedTask;
        }

        private async Task ProcessUploadVideoEvents()
        {
            var connection = await _messageBus.GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            //while (true)
            {
                try
                {
                    using var scope = _serviceScope.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IProfileEntity>>();

                    //var events = await context.Get<VideoUploadEvent>()
                    //    .Where(x => x.State == EventState.New)
                    //    .Take(10)
                    //    .ToListAsync();

                    await _messageBus.SubscribeAsync<VideoUploadEvent>(channel, "quueue", async (e) =>
                    {
                        var storage = _fileStorageFactory.CreateFileStorage();
                        await ProcessFile(storage, e);
                    });

                    //    if (events.Count != 0)
                    //    {
                    //        var storage = _fileStorageFactory.CreateFileStorage();

                    //        foreach (var @event in events)
                    //        {
                    //            await ProcessFile(storage, @event);
                    //        }

                    //    }
                    //    else
                    //    {
                    //        await Task.Delay(1000);
                    //    }
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
                .FirstOrDefaultAsync(x => x.ObjectName == @event.ObjectName);

            if (fileMetadata == null)
            {
                context.Attach(@event);
                @event.SetErrorMessage("Не удалось найти данные");
                await context.SaveChangesAsync();
                return;
            }

            var url = await storage.GetFileUrlAsync(fileMetadata.PostId, @event.ObjectName);

            var videoSizes = new List<VideoSize>() { VideoSize.Hd1080, VideoSize.Hd720, VideoSize.Vga, VideoSize.Nhd };
            var dir = Path.Combine(_tempPath, fileMetadata.Id.ToString());
            var fileId = GuidService.GetNewGuid();
            var inputUrl = new Uri(url).AbsoluteUri;
            var videoStream = await FFMpegService.GetVideoMediaInfo(inputUrl);
            if (videoStream == null)
            {
                context.Attach(@event);
                @event.State = EventState.Error;
                @event.ErrorMessage = "Не удалось найти видеопоток";
                await context.SaveChangesAsync();
                return;
            }
            try
            {
                Directory.CreateDirectory(dir);

                var presets = (videoStream == null
                    ? _videoPresets
                    : _videoPresets.Where(x => x.Width <= videoStream.Width))
                    .ToList();

                await FFMpegService.CreateHls(
                    inputUrl,
                    dir,
                    presets.Select(x => x.GetResolution()).ToArray(),
                    presets.Select(x => x.VideoBitrate).ToArray(),
                    presets.Select(x => x.AudioBitrate).ToArray(),
                    fileId.ToString(), fileMetadata.Id.ToString());

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

            var post = await context.Get<Post>()
                       .FirstAsync(x => x.Id == fileMetadata.PostId);

            if (post.Type == PostType.Video && string.IsNullOrWhiteSpace(post.PreviewId))
            {
                var snapshotFileId = GuidService.GetNewGuid();
                var snapshotFileName = Path.Combine(_tempPath, snapshotFileId.ToString() + ".png");

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
            @event.State = EventState.Complete;

            context.Attach(fileMetadata);
            fileMetadata.IsProcessed = false;
            fileMetadata.ObjectName = $"{fileMetadata.Id}.m3u8";
            fileMetadata.Duration = videoStream.Duration;

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
