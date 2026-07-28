using Blog.Contracts.Events;
using Blog.Domain.Entities;
using FFmpeg.Service;
using FFmpeg.Service.Models;
using Infrastructure.Services;
using MessageBus;
using System.Diagnostics;
using Shared.Services;
using Shared.Utils;

namespace VideoProcessing.Cli.Service;

public sealed class VideoConversionService
{
    private readonly IVideoConvertService _ffmpegService;
    private readonly IFileStorage _storage;
    private readonly string _tempPath;
    private readonly HlsVideoPresets _videoPresets;

    public VideoConversionService(IVideoConvertService ffmpegService, IFileStorageFactory storage, IConfiguration configuration, HlsVideoPresets videoPresets)
    {
        _ffmpegService = ffmpegService;
        _storage = storage.CreateFileStorage();
        _tempPath = Path.GetFullPath(configuration["TempDir"]!);
        _videoPresets = videoPresets;
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
                await ProcessPreviewAsync(command, result, url, videoStream);
            }

            await ProcessHls(command.BlogId, command.VideoMetadata, dir, fileId, url, videoStream);

            result.IsProcessing = false;
            result.ObjectName = $"{postId}/{command.VideoMetadataId}.m3u8";
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

    private async Task ProcessPreviewAsync(ConvertVideoCommand command, VideoConvertedResponse result, string url, FFProbeStream videoStream)
    {
        var snapshotFileId = GuidService.GetNewGuid();
        var snapshotFileName = Path.Combine(_tempPath, snapshotFileId.ToString() + ".jpg");

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
                Name = snapshotFileName,
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
                Console.WriteLine($"Не удалось удалить временный файл превью: {ioEx.Message}");
            }

            // Перезапускаем с полным стеком трассировки
            throw new OperationCanceledException($"Ошибка при создании превью: {exception.Message}", exception);
        }
        catch (Exception exception)
        {
            try
            {
                Console.WriteLine($"Ошибка при генерации превью: {exception.Message}");
            }
            catch { /* Ignore logging errors */ }

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
                        Console.WriteLine($"Preview temp file deleted successfully");
                    }
                    catch (IOException ioEx) when (attempt < 2)
                    {
                        // Файл занят, пробуем снова с задержкой
                        await Task.Delay(100 * (attempt + 1));
                    }
                }

                if (!deleted)
                {
                    Console.WriteLine($"Не удалось удалить временный файл превью: {snapshotFileName}. Будет удалён при следующей попытке или перезапуске.");
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

            // Загружаем папки m3u8 (используем EnumerateDirectories для lazy evaluation)
            foreach (var folder in Directory.EnumerateDirectories(dir))
            {
                var folderName = Path.GetFileName(folder);
                await UploadM3U8Folder(blogId, $"{fileMetadata.PostId}/{folderName}", folder);
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
            // Гарантированная очистка временной директории с повторными попытками
            await CleanupDirectoryAsync(dir);
        }
    }

    /// <summary>
    /// Очищает временную директорию с повторными попытками при ошибках доступа
    /// </summary>
    private static async Task CleanupDirectoryAsync(string directoryPath)
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
                    Console.WriteLine($"Directory already cleaned: {directoryPath}");
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
                    catch (IOException ioEx) when (!(ioEx is TaskCanceledException))
                    {
                        Console.WriteLine($"Не удалось удалить файл: {file} - {ioEx.Message}");
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
                    catch (IOException ioEx) when (!(ioEx is TaskCanceledException))
                    {
                        Console.WriteLine($"Не удалось удалить директорию: {subDir} - {ioEx.Message}");
                    }
                }

                // Удаляем корневую директорию
                Directory.Delete(directoryPath);
                deleted = true;
                Console.WriteLine($"Temp directory cleaned successfully: {directoryPath}");
            }
            catch (IOException ioEx) when (!(ioEx is TaskCanceledException))
            {
                if (attempt < 4)
                {
                    await Task.Delay(200 * (attempt + 1));
                    Console.WriteLine($"Ретрайт очистки директории (попытка {attempt + 1}/{5}): {ioEx.Message}");
                }
            }
        }

        if (!deleted && Directory.Exists(directoryPath))
        {
            Console.WriteLine($"⚠️ Не удалось очистить временную директорию: {directoryPath}. Будет удалена при следующей попытке или перезапуске сервиса.");
        }
    }

    private async Task UploadFilesToStorage(Guid blogId, Guid postId, string dir, string filter)
    {
        var uploadedCount = 0;
        var failedCount = 0;

        foreach (var file in Directory.EnumerateFiles(dir, filter))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var fileName = Path.GetFileName(file);
                var objectName = $"{postId}/{fileName}";

                Console.WriteLine($"Uploading: {objectName}...");
                await _storage.PutFileAsync(blogId, objectName, fileStream);
                uploadedCount++;

                // Небольшая задержка для rate limiting S3
                if (uploadedCount % 10 == 0)
                    await Task.Delay(100);
            }
            catch (Exception ex) when (!(ex is TaskCanceledException))
            {
                failedCount++;
                Console.WriteLine($"Ошибка при загрузке файла {file}: {ex.Message}");
                // Продолжаем с другими файлами, не выбрасывая исключение
            }
        }

        Console.WriteLine($"Загрузка завершена: {uploadedCount} успешно, {failedCount} пропущено");
    }

    private async Task UploadM3U8Folder(Guid blogId, string directoryPrefix, string directoryPath)
    {
        var folderName = Path.GetFileName(directoryPath);
        Console.WriteLine($"Processing m3u8 folder: {folderName}");

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.m3u8"))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var relativeFileName = Path.GetFileName(file).Replace(Path.DirectorySeparatorChar, '/');
                var objectName = $"{directoryPrefix}/{relativeFileName}";

                Console.WriteLine($"Uploading m3u8: {objectName}...");
                await _storage.PutFileAsync(blogId, objectName, fileStream);
            }
            catch (Exception ex) when (!(ex is TaskCanceledException))
            {
                Console.WriteLine($"Ошибка при загрузке m3u8 файла {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Folder {folderName} processed successfully");
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
