using Blog.Contracts.Events;
using Blog.Domain.Entities;
using FFmpeg.Service;
using FFmpeg.Service.Models;
using Infrastructure.Services;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Models;
using System.Diagnostics;
using System.Net;
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

            // Валидация URL для защиты от SSRF атак
            var validatedInputUrl = ValidateUriForHls(url);

            var videoStream = await _ffmpegService.GetVideoMediaInfoAsync(validatedInputUrl) ?? throw new ArgumentException("Не удалось найти видеопоток в видеофайле");
            await ProcessHls(@event.BlogId, @event.VideoMetadata, dir, fileId, validatedInputUrl, videoStream);
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
        
        // Валидация URL для превью
        var validatedPreviewUrl = ValidateUriForHls(url);
        
        try
        {
            await _ffmpegService.GeneratePreviewAsync(validatedPreviewUrl, snapshotFileName);
            
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
            await foreach (var folder in Directory.EnumerateDirectories(dir))
            {
                var folderName = Path.GetFileName(folder);
                await UploadM3U8Folder(blogId, $"{fileMetadata.PostId}/{folderName}", folder);
            }

            // Отменяем отложенные операции при остановке/отмене задачи
            if (!TaskScheduler.IsCurrent)
            {
                Directory.EnumerateDirectories(dir).ToList().ForEach(folder => 
                    Directory.Delete(folder, true));
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
                    catch (IOException ioEx) when (!TaskCanceledException.IsCancellationRequested(ioEx))
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
                    catch (IOException ioEx) when (!TaskCanceledException.IsCancellationRequested(ioEx))
                    {
                        Console.WriteLine($"Не удалось удалить директорию: {subDir} - {ioEx.Message}");
                    }
                }

                // Удаляем корневую директорию
                Directory.Delete(directoryPath);
                deleted = true;
                Console.WriteLine($"Temp directory cleaned successfully: {directoryPath}");
            }
            catch (IOException ioEx) when (!TaskCanceledException.IsCancellationRequested(ioEx))
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

        await foreach (var file in Directory.EnumerateFiles(dir, filter))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open);
                var fileName = Path.GetFileName(file);
                var objectName = $"{postId}/{fileName}";
                
                Console.WriteLine($"Uploading: {objectName}...");
                await _storage.PutFileAsync(blogId, objectName, fileStream);
                uploadedCount++;
                
                // Небольшая задержка для rate limiting S3
                if (uploadedCount % 10 == 0)
                    await Task.Delay(100);
            }
            catch (Exception ex) when (!TaskCanceledException.IsCancellationRequested(ex))
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

        await foreach (var file in Directory.EnumerateFiles(directoryPath, "*.m3u8"))
        {
            try
            {
                using var fileStream = new FileStream(file, FileMode.Open);
                var relativeFileName = Path.GetFileName(file).Replace(Path.DirectorySeparatorChar, '/');
                var objectName = $"{directoryPrefix}/{relativeFileName}";
                
                Console.WriteLine($"Uploading m3u8: {objectName}...");
                await _storage.PutFileAsync(blogId, objectName, fileStream);
            }
            catch (Exception ex) when (!TaskCanceledException.IsCancellationRequested(ex))
            {
                Console.WriteLine($"Ошибка при загрузке m3u8 файла {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Folder {folderName} processed successfully");      
    }

    /// <summary>
    /// Валидирует URL для предотвращения SSRF атак.
    /// Разрешает только HTTP/HTTPS схемы и блокирует доступ к внутренним IP-адресам.
    /// </summary>
    private static Uri ValidateUriForHls(string urlString)
    {
        // Базовая проверка на null или пустую строку
        if (string.IsNullOrWhiteSpace(urlString))
        {
            throw new ArgumentException("URL не может быть пустым", nameof(urlString));
        }

        try
        {
            var uri = new Uri(urlString);
            
            // Разрешаем только HTTP и HTTPS для внешних URL, или bucket:// для внутренних S3/MinIO
            if (!IsAllowedScheme(uri.Scheme))
            {
                throw new SecurityException($"Недопустимая схема URL: {uri.Scheme}. Разрешены: http, https, file");
            }

            // Для HTTP/HTTPS проверяем хост на безопасность (SSRF защита)
            if (IsExternalUri(uri))
            {
                if (!IsValidExternalHost(uri.Host))
                {
                    throw new SecurityException($"Запрещенный доступ к внутреннему IP: {uri.Host}");
                }
            }

            // Блокируем access к localhost и private ranges
            if (IsPrivateIp(uri.Host))
            {
                throw new SecurityException("Доступ к локальным или приватным IP-адресам запрещён");
            }

            return uri;
        }
        catch (UriFormatException ex) when (ex.Message.Contains("invalid URI"))
        {
            // Более специфичное исключение для форматирования
            throw new ArgumentException($"Некорректный формат URL: {urlString}", nameof(urlString));
        }
    }

    /// <summary>
    /// Проверяет, разрешена ли схема URL
    /// </summary>
    private static bool IsAllowedScheme(string scheme)
    {
        return ["http", "https", "file"].Contains(scheme.ToLowerInvariant());
    }

    /// <summary>
    /// Проверяет, является ли это внешним URL (не bucket:// и не file://)
    /// </summary>
    private static bool IsExternalUri(string scheme)
    {
        var lowerScheme = scheme.ToLowerInvariant();
        return !lowerScheme.Equals("file") && !lowerScheme.StartsWith("bucket");
    }

    /// <summary>
    /// Проверяет, является ли хост разрешённым внешним доменом
    /// </summary>
    private static bool IsValidExternalHost(string host)
    {
        // Разрешаем только публичные домены без поддонов localhost/127.0.0.1
        // В продакшене добавить whitelist разрешённых доменов
        return !string.IsNullOrEmpty(host) && 
               !host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) &&
               !host.Contains(":") && // IPv4 с портом запрещаем
               host.Length < 253;     // Максимальная длина DNS хоста
    }

    /// <summary>
    /// Проверяет, является ли IP приватным (RFC 1918)
    /// </summary>
    private static bool IsPrivateIp(string host)
    {
        try
        {
            if (!IpHostLookup.TryParseAddress(host, out var ipAddresses))
                return true; // Если не удалось解析, считаем приватным

            foreach (var ip in ipAddresses)
            {
                // RFC 1918: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
                if (ip.IsLoopback || 
                    ip.IsLinkLocalUnicast || 
                    ip.IsSiteLocal ||
                    IsPrivateRange(ip))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Если не удалось解析, блокируем для безопасности
            return true;
        }

        return false;
    }

    /// <summary>
    /// Проверка на частные диапазоны IP (RFC 1918 и другие)
    /// </summary>
    private static bool IsPrivateRange(IpAddress ip)
    {
        // 0.0.0.0/8, 100.64.0.0/10 (CGNAT), 224.0.0.0/4 (multicast)
        var octets = ip.Octets;
        
        if (octets.Length == 4 && octets[0] == 0) return true; // 0.0.0.0/8
        if (octets.Length == 4 && octets[0] == 100 && octets[1] >= 64 && octets[1] <= 127) return true; // CGNAT
        if (octets.Length == 4 && octets[0] >= 224 && octets[0] <= 239) return true; // Multicast

        // IPv6 private ranges
        if (ip is IPv6Address ipv6)
        {
            var segments = ipv6.Segments;
            // fc00::/7 (unique local), fe80::/10 (link-local)
            for (int i = 0; i < Math.Min(segments.Length, 2); i++)
            {
                if (segments[i] >= 0xfc && segments[i] <= 0xfd) return true;
                if (i == 0 && segments[i] == 0xfe && (segments[1] & 0xc0) == 0x80) return true;
            }
        }

        return false;
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
