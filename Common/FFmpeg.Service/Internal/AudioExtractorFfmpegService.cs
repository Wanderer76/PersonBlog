using FFmpeg.Service.Models;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FFmpeg.Service.Internal;

public class AudioExtractorFfmpegService : IAudioExtractorService
{

    private readonly ILogger<AudioExtractorFfmpegService> _logger;
    private readonly FFMpegOptions fFMpegOptions;

    public AudioExtractorFfmpegService(ILogger<AudioExtractorFfmpegService> logger, FFMpegOptions fFMpegOptions)
    {

        _logger = logger;
        this.fFMpegOptions = fFMpegOptions;
    }

    public async Task<AudioFileMetadata> ExtractMetadataAsync(IFormFile mp3File)
    {
        var tempFilePath = await SaveTempFileAsync(mp3File);

        try
        {
            var metadata = new AudioFileMetadata
            {
                OriginalFileName = mp3File.FileName,
                FileSize = mp3File.Length
            };

            // Извлекаем информацию через ffmpeg
            var info = await GetMediaInfoAsync(tempFilePath);

            // Парсим метаданные
            metadata.Title = GetTagValue(info, "title") ?? Path.GetFileNameWithoutExtension(mp3File.FileName);
            metadata.Artist = GetTagValue(info, "artist");
            metadata.Album = GetTagValue(info, "album");
            metadata.Year = GetTagValue(info, "date");
            metadata.Genre = GetTagValue(info, "genre");
            metadata.Duration = ParseDurationWithTimeSpan(GetDuration(info));
            metadata.Bitrate = GetBitrate(info);

            // Проверяем и извлекаем обложку
            metadata.HasCover = await HasCoverArtAsync(tempFilePath);
            if (metadata.HasCover)
            {
                metadata.CoverBase64 = await ExtractCoverToBase64Async(tempFilePath);
                metadata.CoverMimeType = "image/jpeg";
            }

            return metadata;
        }
        finally
        {
            CleanupTempFile(tempFilePath);
        }
    }

    public async Task<string> ExtractCoverToBase64Async(string filePath)
    {
        var tempCoverPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fFMpegOptions.FFMpegPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-y");
            process.StartInfo.ArgumentList.Add("-i");
            process.StartInfo.ArgumentList.Add(filePath);
            process.StartInfo.ArgumentList.Add("-map");
            process.StartInfo.ArgumentList.Add("0:v:0");
            process.StartInfo.ArgumentList.Add("-frames:v");
            process.StartInfo.ArgumentList.Add("1");
            process.StartInfo.ArgumentList.Add("-c:v");
            process.StartInfo.ArgumentList.Add("mjpeg");
            process.StartInfo.ArgumentList.Add(tempCoverPath);

            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await WaitForExitWithTimeoutAsync(process);
            _ = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"FFmpeg cover extraction failed with exit code {process.ExitCode}: {error}");

            if (File.Exists(tempCoverPath) && new FileInfo(tempCoverPath).Length > 0)
            {
                var imageBytes = await File.ReadAllBytesAsync(tempCoverPath);
                return Convert.ToBase64String(imageBytes);
            }

            return null;
        }
        finally
        {
            if (File.Exists(tempCoverPath))
                File.Delete(tempCoverPath);
        }
    }

    public async Task<bool> ValidateMp3FileAsync(string filePath)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fFMpegOptions.FFMpegPath,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.StartInfo.ArgumentList.Add("-v");
            process.StartInfo.ArgumentList.Add("error");
            process.StartInfo.ArgumentList.Add("-i");
            process.StartInfo.ArgumentList.Add(filePath);
            process.StartInfo.ArgumentList.Add("-f");
            process.StartInfo.ArgumentList.Add("null");
            process.StartInfo.ArgumentList.Add("-");

            process.Start();
            var errorTask = process.StandardError.ReadToEndAsync();
            await WaitForExitWithTimeoutAsync(process);
            var errorOutput = await errorTask;

            if (process.ExitCode != 0)
                _logger.LogWarning("MP3 validation failed with exit code {ExitCode}: {Error}", process.ExitCode, errorOutput);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetMediaInfoAsync(string filePath)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fFMpegOptions.FFMpegPath,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add(filePath);

        process.Start();
        var outputTask = process.StandardError.ReadToEndAsync();
        await WaitForExitWithTimeoutAsync(process);
        string output = await outputTask;

        return output;
    }

    private async Task<bool> HasCoverArtAsync(string filePath)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fFMpegOptions.FFProbePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add(filePath);
        process.StartInfo.ArgumentList.Add("-show_streams");
        process.StartInfo.ArgumentList.Add("-select_streams");
        process.StartInfo.ArgumentList.Add("v:0");
        process.StartInfo.ArgumentList.Add("-loglevel");
        process.StartInfo.ArgumentList.Add("error");

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await WaitForExitWithTimeoutAsync(process);
        var output = await outputTask;
        _ = await errorTask;

        return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
    }

    private async Task<string> SaveTempFileAsync(IFormFile file)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));

        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return tempFilePath;
    }

    private void CleanupTempFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temp file"); }
        }
    }

    private async Task WaitForExitWithTimeoutAsync(Process process)
    {
        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(1, fFMpegOptions.CommandTimeoutSeconds)));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the checks.
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to terminate timed out process {Process}", process.StartInfo.FileName);
            }

            throw new TimeoutException(
                $"Process '{Path.GetFileName(process.StartInfo.FileName)}' exceeded the configured timeout of {fFMpegOptions.CommandTimeoutSeconds} seconds.");
        }
    }

    private string GetTagValue(string info, string tagName)
    {
        var pattern = $@"{tagName}\s*: (.+)";
        var match = Regex.Match(info, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string GetDuration(string info)
    {
        var match = Regex.Match(info, @"Duration: (\d{2}:\d{2}:\d{2}\.\d{2})");
        return match.Success ? match.Groups[1].Value : "00:00:00";
    }

    private string GetBitrate(string info)
    {
        var match = Regex.Match(info, @"bitrate: (\d+ kb/s)");
        return match.Success ? match.Groups[1].Value : "0 kb/s";
    }

    private static long ParseDurationWithTimeSpan(string durationString)
    {
        if (string.IsNullOrEmpty(durationString))
            return 0;

        try
        {
            var cleanDuration = durationString.Contains('.')
                ? durationString.Substring(0, durationString.IndexOf('.'))
                : durationString;

            if (TimeSpan.TryParse(cleanDuration, out var timeSpan))
            {
                return (long)timeSpan.TotalMilliseconds;
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }
}
