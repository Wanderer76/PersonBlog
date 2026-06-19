using FFmpeg.Service.Models;
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

    public async Task<Result<FileMetadataModel>> ConvertImageToPngAsync(FileMetadataModel image) 
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
                await image.ContentStream.CopyToAsync(fs);
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
            var errorBuilder = new System.Text.StringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginErrorReadLine();

            var exited = await Task.Run(() => process.WaitForExit(60000)); // таймаут 60 сек
            if (!exited || process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg error: {Error}", errorBuilder.ToString());
                throw new InvalidOperationException(
                    $"FFmpeg failed (exit code {process.ExitCode}): {errorBuilder}");
            }

            // 3. Считываем результат
            var bytes =  await File.ReadAllBytesAsync(outputPath);
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
}
