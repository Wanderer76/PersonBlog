using FFmpeg.Service.Models;
using FileStorage.Service;
using Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FFmpeg.Service.Internal;
internal sealed class FFmpegImageConvertService : IImageConvertService
{
    private readonly FFMpegOptions options;
    private readonly ILogger<FFmpegImageConvertService> _logger;
    public FFmpegImageConvertService(FFMpegOptions options, ILogger<FFmpegImageConvertService> logger)
    {
        this.options = options;
        _logger = logger;
    }

    public async Task<Result<FileMetadataModel>> ConvertImageToPngAsync(FileMetadataModel image, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(image.FileExtension))
            return Result<FileMetadataModel>.Failure(new Shared.Utils.Error("image","Имя файла пусто"));

        var inputPath = Path.Combine(options.TempPath, $"{Guid.NewGuid()}{image.FileExtension}");
        var outputPath = Path.Combine(options.TempPath, $"{Guid.NewGuid()}.png");

        try
        {
            // 1. Сохраняем исходный файл
            await using (var fs = new FileStream(inputPath, FileMode.Create, FileAccess.Write))
            {
                await image.ContentStream.CopyToAsync(fs, cancellationToken);
            }

            // 2. Запускаем ffmpeg
            var startInfo = new ProcessStartInfo
            {
                FileName = options.FFMpegPath,
                Arguments = $"-y -i \"{inputPath}\" \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                await TerminateProcessAsync(process);
                throw new TimeoutException("FFmpeg image conversion exceeded the 60 second timeout.");
            }
            catch (OperationCanceledException)
            {
                await TerminateProcessAsync(process);
                throw;
            }

            _ = await outputTask;
            var standardError = await errorTask;
            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg exited with code {ExitCode}: {Error}", process.ExitCode, standardError);
                throw new InvalidOperationException($"FFmpeg failed (exit code {process.ExitCode}): {standardError}");
            }

            // 3. Считываем результат
            var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
            var resultName = Path.GetFileNameWithoutExtension(image.FileName) + ".png";

            return new FileMetadataModel
            {
                ContentStream = new MemoryStream(bytes),
                ContentType = "image/png",
                FileExtension = ".png",
                FileName = resultName,
                Length = bytes.Length,
                Name = image.Name,
            };
        }
        finally
        {
            // 4. Чистим временные файлы
            TryDelete(inputPath);
            TryDelete(outputPath);
        }
    }
    private void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) { _logger.LogWarning(ex, "Cannot delete {Path}", path); }
    }

    private static async Task TerminateProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process exited between HasExited and Kill.
            return;
        }
        catch
        {
            return;
        }

        using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await process.WaitForExitAsync(waitCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Do not keep the HTTP request open if the child process cannot be reaped.
        }
    }
}
