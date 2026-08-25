using Blog.Contracts.Events;
using Blog.Domain.Entities;
using FFmpeg.Service.Models;
using Infrastructure.Services;
using MessageBus;
using System.Diagnostics;
using Shared.Services;
using Shared.Utils;
using FileStorage.Service;
using FileStorage.Service.Models;

namespace VideoProcessing.Cli.Service;

public sealed class VideoConversionService
{
    private readonly IVideoConvertService _ffmpegService;
    private readonly IFileStorage _storage;
    private readonly string _tempPath;
    private readonly HlsVideoPresets _videoPresets;
    private readonly IVideoProgressNotifier _progressNotifier;
    private readonly ILogger<VideoConversionService> _logger;

    public VideoConversionService(
        IVideoConvertService ffmpegService,
        IFileStorage storage,
        IConfiguration configuration,
        HlsVideoPresets videoPresets,
        IVideoProgressNotifier progressNotifier,
        ILogger<VideoConversionService> logger)
    {
        _ffmpegService = ffmpegService;
        _storage = storage;
        _tempPath = Path.GetFullPath(configuration["TempDir"]!);
        _videoPresets = videoPresets;
        _progressNotifier = progressNotifier;
        _logger = logger;
    }

    public async Task<VideoConvertedResponse> ProcessConversionAsync(ConvertVideoCommand command, Guid postId, bool hasPreviewId)
    {
        var result = new VideoConvertedResponse
        {
            PostId = postId,
            VideoMetadataId = command.VideoMetadataId,
        };

        try
        {
            var url = await _storage.GetFileUrlAsync(command.BlogId, command.ObjectName);
            var dir = Path.Combine(_tempPath, command.VideoMetadataId.ToString());
            var fileId = GuidService.GetNewGuid();

            // Упрощенная валидация URL (проверка на null/пустоту)
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL не может быть пустым", nameof(url));
            }

            var videoStream = await _ffmpegService.GetVideoMediaInfoAsync(url) ?? throw new ArgumentException("Не удалось найти видеопоток в видеофайле");

            if (!hasPreviewId)
            {
                await ProcessPreviewAsync(command, result, url);
            }

            await ReportProgressSafelyAsync(command.BlogId, postId, 0, "processing");
            await ProcessHls(command.BlogId, command.VideoMetadata, dir, fileId, url, videoStream, postId);

            result.IsProcessing = false;
            result.ObjectName = $"{postId}/{command.VideoMetadataId}.m3u8";
            result.Duration = videoStream.Duration;
            result.ProcessState = ProcessState.Complete;
            await ReportProgressSafelyAsync(command.BlogId, postId, 100, "completed");
            return result;
        }
        catch (Exception e)
        {
            result.Error = $"Ошибка конвертации видео: {e.Message}";
            result.ProcessState = ProcessState.Error;
            await ReportProgressSafelyAsync(command.BlogId, postId, 0, "failed", result.Error);
            return result;
        }
    }

    private async Task ProcessPreviewAsync(ConvertVideoCommand command, VideoConvertedResponse result, string url)
    {
        var snapshotFileId = GuidService.GetNewGuid();
        var snapshotFileName = Path.Combine(_tempPath, snapshotFileId.ToString() + ".png");

        try
        {
            await _ffmpegService.GeneratePreviewAsync(url, snapshotFileName);

            using var fileStream = new FileStream(snapshotFileName, FileMode.Open);
            using var copyStream = new MemoryStream();
            await fileStream.CopyToAsync(copyStream);
            copyStream.Position = 0;

            var objectName = await _storage.PutFileAsync(command.BlogId, $"{command.PostId}/{snapshotFileId.ToString()}", copyStream);

            result.PreviewId = new Shared.Models.BaseFileMetadataEntity
            {
                CreatedAt = DateTimeService.Now(),
                ContentType = "image/png",
                FileExtension = ".png",
                Id = snapshotFileId,
                Length = copyStream.Length,
                Name = Path.GetFileName(snapshotFileName),
                ObjectName = objectName
            };
        }
        catch (Exception exception) when (File.Exists(snapshotFileName))
        {
            try
            {
                // Если файл превью был создан но не удалось загрузить в S3 — удаляем его
                File.Delete(snapshotFileName);
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, "Не удалось удалить временный файл превью {SnapshotFileName}", snapshotFileName);
            }

            // Перезапускаем с полным стеком трассировки
            throw new OperationCanceledException($"Ошибка при создании превью: {exception.Message}", exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ошибка при генерации превью");

            // Перезапускаем с полным стеком трассировки, сохраняя оригинальное исключение внутри
            throw new OperationCanceledException("Не удалось создать превью к видео", exception);
        }
        finally
        {
            if (File.Exists(snapshotFileName))
            {
                // Пытаемся удалить файл с задержкой на случай lock-файла
                var deleted = false;
                for (var attempt = 0; attempt < 3 && !deleted; attempt++)
                {
                    try
                    {
                        File.Delete(snapshotFileName);
                        deleted = true;
                        _logger.LogDebug("Preview temp file {SnapshotFileName} deleted successfully", snapshotFileName);
                    }
                    catch (IOException) when (attempt < 2)
                    {
                        // Файл занят, пробуем снова с задержкой
                        await Task.Delay(100 * (attempt + 1));
                    }
                }

                if (!deleted)
                {
                    _logger.LogWarning("Не удалось удалить временный файл превью {SnapshotFileName}; он будет удалён при следующей попытке или перезапуске", snapshotFileName);
                }
            }
        }
    }

    private async Task ProcessHls(Guid blogId, VideoFile fileMetadata, string dir, Guid fileId, string inputUrl, VideoMediaInfo videoStream, Guid postId)
    {
        try
        {
            Directory.CreateDirectory(dir);

            var presets = _videoPresets.VideoPresets
                .Where(x => x.Width <= videoStream.Width)
                .ToList();

            if (presets.Count == 0)
                throw new InvalidOperationException($"Не найден HLS-пресет для ширины видео {videoStream.Width}px.");

            var hlsOptions = new HlsOptions
            {
                Resolutions = [.. presets.Select(x => x.GetResolution())],
                Bitrates = [.. presets.Select(x => x.VideoBitrate)],
                AudioBitrates = [.. presets.Select(x => x.AudioBitrate)],
                SegmentFileName = fileId.ToString(),
                MasterName = fileMetadata.Id.ToString(),
                EncodePreset = _videoPresets.EncodePreset
            };

            var progressCallBack = new AsyncProgress<double>(async currentTime =>
            {
                var percent = videoStream.Duration <= 0
                    ? 0
                    : Math.Min(100, currentTime / videoStream.Duration * 100);
                _logger.LogDebug("Video processing progress: {Percent}", percent);
                await ReportProgressSafelyAsync(blogId, postId, percent, "processing");
            });

            await _ffmpegService.CreateHlsAsync(inputUrl, dir, hlsOptions, progressCallBack);

            var masterPlaylistPath = Path.Combine(dir, $"{hlsOptions.MasterName}.m3u8");
            if (!File.Exists(masterPlaylistPath))
                throw new InvalidOperationException("FFmpeg завершился без создания master HLS playlist.");

            // Загружаем корневой master.m3u8
            foreach (var file in Directory.GetFiles(dir))
            {
                using var fileStream = new FileStream(file, FileMode.Open);
                var objectName = await _storage.PutFileAsync(blogId, $"{fileMetadata.PostId}/{Path.GetFileName(file)}", fileStream);
            }

            foreach (string folder in Directory.EnumerateDirectories(dir))
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    using var fileStream = new FileStream(file, FileMode.Open);
                    var objectName = await _storage.PutFileAsync(blogId, $"{fileMetadata.PostId}/{GetRelativePath(file).Replace(Path.DirectorySeparatorChar, '/')}", fileStream);
                }
            }

            // Отменяем отложенные операции при остановке/отмене задачи
            if (Task.CurrentId != null)
            {
                foreach (var folder in Directory.EnumerateDirectories(dir))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
        catch (Exception)
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
                    _logger.LogWarning("Не удалось очистить директорию {DirectoryPath}", dir);
                }
            }
            throw;
        }
        finally
        {
            // Гарантированная очистка временной директории с повторными попытками
            await CleanupDirectoryAsync(dir);
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

    /// <summary>
    /// Очищает временную директорию с повторными попытками при ошибках доступа
    /// </summary>
    private async Task CleanupDirectoryAsync(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return;

        var deleted = false;
        for (var attempt = 0; attempt < 5 && !deleted; attempt++)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogDebug("Directory {DirectoryPath} is already cleaned", directoryPath);
                    return;
                }

                // Сначала удаляем все файлы внутри
                var filesToClean = Directory.GetFiles(directoryPath);
                foreach (var file in filesToClean)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogWarning(ioEx, "Не удалось удалить файл {FilePath}", file);
                    }
                }

                // Удаляем поддиректории
                var subDirectories = Directory.GetDirectories(directoryPath);
                foreach (var subDir in subDirectories)
                {
                    try
                    {
                        Directory.Delete(subDir, true);
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogWarning(ioEx, "Не удалось удалить директорию {DirectoryPath}", subDir);
                    }
                }

                // Удаляем корневую директорию
                Directory.Delete(directoryPath);
                deleted = true;
                _logger.LogDebug("Temp directory {DirectoryPath} cleaned successfully", directoryPath);
            }
            catch (IOException ioEx)
            {
                if (attempt < 4)
                {
                    await Task.Delay(200 * (attempt + 1));
                    _logger.LogWarning(ioEx, "Повторная попытка очистки директории {DirectoryPath}: {Attempt}/{MaxAttempts}", directoryPath, attempt + 1, 5);
                }
            }
        }

        if (!deleted && Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Не удалось очистить временную директорию {DirectoryPath}; она будет удалена при следующей попытке или перезапуске сервиса", directoryPath);
        }
    }

    private async Task ReportProgressSafelyAsync(
        Guid blogId,
        Guid postId,
        double percent,
        string status,
        string? error = null)
    {
        try
        {
            await _progressNotifier.ReportAsync(blogId, postId, percent, status, error);
        }
        catch (Exception exception)
        {
            // A disconnected UI must not fail or cancel the video conversion itself.
            _logger.LogWarning(exception, "Unable to publish video progress for post {PostId}", postId);
        }
    }

    private async Task UploadM3U8MasterPlaylist(Guid blogId, Guid postId, string masterPlaylistPath)
    {
        try
        {
            using var fileStream = new FileStream(masterPlaylistPath, FileMode.Open, FileAccess.Read);
            var objectName = $"{postId}/master.m3u8";

            _logger.LogDebug("Uploading master playlist {ObjectName}", objectName);
            await _storage.PutFileAsync(blogId, objectName, fileStream);
        }
        catch (Exception ex) when (!(ex is TaskCanceledException))
        {
            _logger.LogError(ex, "Ошибка при загрузке master playlist для поста {PostId}", postId);
        }
    }

    private async Task UploadM3U8Playlist(Guid blogId, Guid postId, string segmentName, string playlistPath)
    {
        try
        {
            using var fileStream = new FileStream(playlistPath, FileMode.Open, FileAccess.Read);
            var objectName = $"{postId}/{segmentName}/playlist.m3u8";

            _logger.LogDebug("Uploading playlist {ObjectName}", objectName);
            await _storage.PutFileAsync(blogId, objectName, fileStream);
        }
        catch (Exception ex) when (!(ex is TaskCanceledException))
        {
            _logger.LogError(ex, "Ошибка при загрузке playlist {SegmentName} для поста {PostId}", segmentName, postId);
        }
    }

    private async Task UploadFilesToStorage(Guid blogId, Guid postId, string dir, string filter)
    {
        var uploadedCount = 0;
        var failedCount = 0;

        // Ищем файлы рекурсивно во всех поддиректориях (FFmpeg создает .ts файлы в папках по разрешениям)
        foreach (var file in Directory.EnumerateFiles(dir, filter, SearchOption.AllDirectories))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var fileName = Path.GetFileName(file);
                var objectName = $"{postId}/{fileName}";

                _logger.LogDebug("Uploading {ObjectName}", objectName);
                await _storage.PutFileAsync(blogId, objectName, fileStream);
                uploadedCount++;

                // Небольшая задержка для rate limiting S3
                if (uploadedCount % 10 == 0)
                    await Task.Delay(100);
            }
            catch (Exception ex) when (!(ex is TaskCanceledException))
            {
                failedCount++;
                _logger.LogError(ex, "Ошибка при загрузке файла {FilePath}", file);
                // Продолжаем с другими файлами, не выбрасывая исключение
            }
        }

        _logger.LogInformation("Загрузка завершена: {UploadedCount} успешно, {FailedCount} пропущено", uploadedCount, failedCount);
    }

}
