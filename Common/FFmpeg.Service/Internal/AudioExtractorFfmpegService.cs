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
                    Arguments = $"-i \"{filePath}\" -map 0:v -c copy \"{tempCoverPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.Error.WriteLine($"Error: {e.Data}");
                }
            };
            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

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
                    FileName = "ffmpeg",
                    Arguments = $"-v error -i \"{filePath}\" -f null -",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return string.IsNullOrEmpty(errorOutput) || !errorOutput.Contains("Invalid data found");
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
                Arguments = $"-i \"{filePath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    private async Task<bool> HasCoverArtAsync(string filePath)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fFMpegOptions.FFProbePath,
                Arguments = $"-i \"{filePath}\" -show_streams -select_streams v -loglevel error",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return !string.IsNullOrEmpty(output);
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