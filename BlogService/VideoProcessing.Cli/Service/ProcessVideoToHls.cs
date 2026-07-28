using Blog.Contracts.Events;
using Blog.Domain.Entities;
using FFmpeg.Service;
using FFmpeg.Service.Models;
using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Models;
using Shared.Services;
using Shared.Utils;

namespace VideoProcessing.Cli.Service;

public sealed class ProcessVideoToHls : IEventHandler<ConvertVideoCommand>
{
    private readonly IVideoConvertService _ffmpegService;
    private readonly IFileStorage _storage;
    private readonly string _tempPath;
    private readonly HlsVideoPresets _videoPresets;

    public ProcessVideoToHls(IVideoConvertService ffmpegService, IFileStorageFactory storage, IConfiguration configuration, HlsVideoPresets videoPresets)
    {
        _ffmpegService = ffmpegService;
        _storage = storage.CreateFileStorage();
        _tempPath = Path.GetFullPath(configuration["TempDir"]!);
        _videoPresets = videoPresets;
    }

    public async Task Handle(IMessageContext<ConvertVideoCommand> @event)
    {
        var result = await HandleConversion(@event.Message);
        await @event.PublishAsync(BaseEvent<VideoConvertedResponse>.Create(result), new() { CorrelationId = result.VideoMetadataId.ToString()});
    }

    private async Task<VideoConvertedResponse> HandleConversion(ConvertVideoCommand @event)
    {
        var result = new VideoConvertedResponse
        {
            PostId = @event.PostId,
            VideoMetadataId = @event.VideoMetadataId,
        };
        try
        {
            var url = await _storage.GetFileUrlAsync(@event.BlogId, @event.ObjectName);
            var dir = Path.Combine(_tempPath, @event.VideoMetadataId.ToString());
            var fileId = GuidService.GetNewGuid();

            var inputUrl = new Uri(url).AbsoluteUri;

            var videoStream = await _ffmpegService.GetVideoMediaInfoAsync(inputUrl) ?? throw new ArgumentException("Не удалось найти видеопоток");
            await ProcessHls(@event.BlogId, @event.VideoMetadata, dir, fileId, inputUrl, videoStream);
            if (!@event.HasPreviewId)
            {
                await ProcessPreviewAsync(@event, result, url, videoStream);
            }

            result.IsProcessing = false;
            result.ObjectName = $"{@event.PostId}/{@event.VideoMetadataId}.m3u8";
            result.Duration = videoStream.Duration;
            result.ProcessState = ProcessState.Complete;
            return result;
        }
        catch (Exception e)
        {
            result.Error = $"Ошибка конвертации видео: {e.Message}";
            result.ProcessState = ProcessState.Error;
            return result;
        }
    }

    private async Task ProcessPreviewAsync(ConvertVideoCommand @event, VideoConvertedResponse result, string url, FFProbeStream videoStream)
    {
        var snapshotFileId = GuidService.GetNewGuid();
        var snapshotFileName = Path.Combine(_tempPath, snapshotFileId.ToString() + ".jpg");
        try
        {
            await _ffmpegService.GeneratePreviewAsync(new Uri(url).AbsoluteUri, snapshotFileName);
            
            using var fileStream = new FileStream(snapshotFileName, FileMode.Open);
            using var copyStream = new MemoryStream();
            await fileStream.CopyToAsync(copyStream);
            copyStream.Position = 0;

            var objectName = await _storage.PutFileAsync(@event.BlogId, $"{@event.PostId}/{snapshotFileId.ToString()}", copyStream);

            result.PreviewId = new Shared.Models.BaseFileMetadataEntity
            {
                CreatedAt = DateTimeService.Now(),
                ContentType = "image/png",
                FileExtension = ".png",
                Id = snapshotFileId,
                Length = copyStream.Length,
                Name = snapshotFileName,
                ObjectName = objectName
            };
        }
        catch (Exception exception) when (File.Exists(snapshotFileName))
        {
            // Если файл превью был создан но не удалось загрузить в S3 — удаляем его
            File.Delete(snapshotFileName);
            throw; // Перезапускаем с полным стеком трассировки
        }
        catch (Exception exception)
        {
            // Для других ошибок оставляем временный файл — очистится при следующей попытке
            Console.WriteLine($"Ошибка при генерации превью: {exception.Message}");
            throw; // Перезапускаем с полным стеком трассировки
        }
        finally
        {
            if (File.Exists(snapshotFileName))
            {
                try
                {
                    File.Delete(snapshotFileName);
                }
                catch (IOException)
                {
                    // Если файл занят (lock от другой задачи), пропустим — очистится позже
                }
            }
        }
    }

    private async Task ProcessHls(Guid blogId, VideoFile fileMetadata, string dir, Guid fileId, string inputUrl, FFProbeStream videoStream)
    {
        try
        {
            Directory.CreateDirectory(dir);

            var presets = (videoStream == null
                ? _videoPresets.VideoPresets
                : _videoPresets.VideoPresets.Where(x => x.Width <= videoStream.Width))
                .ToList();

            var hlsOptions = new HlsOptions
            {
                Resolutions = [.. presets.Select(x => x.GetResolution())],
                Bitrates = [.. presets.Select(x => x.VideoBitrate)],
                AudioBitrates = [.. presets.Select(x => x.AudioBitrate)],
                SegmentFileName = fileId.ToString(),
                MasterName = fileMetadata.Id.ToString(),
                EncodePreset = _videoPresets.EncodePreset
            };

            var progressCallBack = new AsyncProgress<double>((currentTime) =>
            {
                var percent = Math.Min(100, currentTime / fileMetadata.Duration * 100);
                Console.WriteLine($"Percent : {percent}");
                return Task.CompletedTask;
            });

            await _ffmpegService.CreateHlsAsync(inputUrl, dir, hlsOptions, progressCallBack);

            // Загружаем файлы в S3/MinIO с обработкой ошибок
            await UploadFilesToStorage(blogId, fileMetadata.PostId, dir, "*.ts");
            
            // Загружаем папки m3u8
            foreach (var folder in Directory.EnumerateDirectories(dir))
            {
                var folderName = Path.GetFileName(folder);
                await UploadM3U8Folder(blogId, $"{fileMetadata.PostId}/{folderName}", folder);
            }
        }
        catch (Exception exception)
        {
            // При ошибке конвертации — очищаем временный директорию
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                    // Если не удалилось сразу, очистится при следующей попытке
                    Console.WriteLine($"Не удалось очистить директорию: {dir}");
                }
            }
            throw;
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (IOException)
                {
                    // Если директория была удалена уже — пропустим
                }
            }
        }
    }

    private async Task UploadFilesToStorage(Guid blogId, Guid postId, string dir, string filter)
    {
        await foreach (var file in Directory.EnumerateFiles(dir, filter))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open);
                var fileName = Path.GetFileName(file);
                var objectName = $"{postId}/{fileName}";
                await _storage.PutFileAsync(blogId, objectName, fileStream);
                Console.WriteLine($"Uploaded: {objectName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке файла {file}: {ex.Message}");
                // Ошибку не выбрасываем — попробуем продолжить с другими файлами
            }
        }
    }

    private async Task UploadM3U8Folder(Guid blogId, string prefix, string folderPath)
    {
        await foreach (var file in Directory.EnumerateFiles(folderPath, "*.m3u8"))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open);
                var relativeFileName = Path.GetFileName(file).Replace(Path.DirectorySeparatorChar, '/');
                var objectName = $"{prefix}/{relativeFileName}";
                await _storage.PutFileAsync(blogId, objectName, fileStream);
                Console.WriteLine($"Uploaded m3u8: {objectName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке m3u8 файла {file}: {ex.Message}");
            }
        }
    }

    private static string GetRelativePath(string filePath)
    {
        var directoryName = Path.GetDirectoryName(filePath);

        if (directoryName != null)
        {
            string[] pathComponents = directoryName.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

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
}
