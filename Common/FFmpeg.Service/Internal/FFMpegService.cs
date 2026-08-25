using FFmpeg.Service.Models;
using FileStorage.Service;
using FileStorage.Service.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Utils;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace FFmpeg.Service.Internal
{
    internal sealed class FFMpegService : IVideoConvertService
    {
        private readonly FFMpegOptions fFMpegOptions;
        private readonly ILogger<FFMpegService> logger;

        public FFMpegService(FFMpegOptions configuration, ILogger<FFMpegService> logger)
        {
            fFMpegOptions = configuration;
            this.logger = logger;
        }

        public async Task GeneratePreviewAsync(string input, string outputFilePath)
        {
            string[] args = ["-ss", "00:00:01", "-i", input, "-frames:v", "1", "-q:v", "2", outputFilePath];
            await ExecuteCommand(fFMpegOptions.FFMpegPath, args);
        }

        public async Task<VideoMediaInfo?> GetVideoMediaInfoAsync(string input)
        {
            var inputMedia = await GetStreams(input);
            var inputVideo = inputMedia.FirstOrDefault(x => x.CodecType == "video");
            return inputVideo == null? null : new VideoMediaInfo(inputVideo.CodecName,inputVideo.Height, inputVideo.Width, inputVideo.CodecType, inputVideo.Duration, inputVideo.BitRate);
        }

        public async Task CreateHlsAsync(string input, string output, HlsOptions options, AsyncProgress<double>? callback)
        {
            options.AssertFound("Опции равны null");
            ValidateHlsOptions(input, output, options);

            var filterComplexBuilder = new StringBuilder();
            filterComplexBuilder.Append("[0:v]split=").Append(options.Resolutions.Count);
            for (int i = 1; i <= options.Resolutions.Count; i++)
            {
                filterComplexBuilder.Append($"[v{i}]");
            }

            filterComplexBuilder.Append(';');
            var mapVariantsBuilder = new StringBuilder();

            var inputMedia = await GetStreams(input);
            var inputAudio = inputMedia.FirstOrDefault(x => x.CodecType == "audio");
            var inputVideo = inputMedia.FirstOrDefault(x => x.CodecType == "video")
                ?? throw new InvalidOperationException("Input does not contain a video stream.");

            for (int i = 0; i < options.Resolutions.Count; i++)
            {
                string resolution = options.Resolutions[i];
                string[] parts = resolution.Split('x');
                int width = int.Parse(parts[0]);
                int height = int.Parse(parts[1]);
                int targetWidth = inputVideo.Width > inputVideo.Height ? width : height;
                int targetHeight = inputVideo.Width > inputVideo.Height ? height : width;
                filterComplexBuilder.Append($"[v{i + 1}]scale=w={targetWidth}:h={targetHeight}:force_original_aspect_ratio=decrease,")
                    .Append($"pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2,setsar=1,format=yuv420p[v{i}out];");

                if (inputAudio != null)
                {
                    mapVariantsBuilder.Append($"v:{i},a:{i} ");
                }
                else
                {
                    mapVariantsBuilder.Append($"v:{i} ");
                }
            }

            var filterComplex = filterComplexBuilder.ToString();
            var mapVariants = mapVariantsBuilder.ToString().Trim();
            var segmentTemplate = Path.Combine(output, $"{options.SegmentFileName}_%v", "data%05d.ts");
            var playlistTemplate = Path.Combine(output, $"{options.SegmentFileName}_%v", "playlist.m3u8");

            for (var i = 0; i < options.Resolutions.Count; i++)
                Directory.CreateDirectory(Path.Combine(output, $"{options.SegmentFileName}_{i}"));

            var ffmpegArguments = new List<string>
            {
                "-hide_banner", "-y", "-i", input,
                "-filter_complex", filterComplex
            };

            for (var i = 0; i < options.Resolutions.Count; i++)
            {
                var rate = options.Bitrates[i];
                ffmpegArguments.AddRange([
                    "-map", $"[v{i}out]",
                    $"-c:v:{i}", fFMpegOptions.DefaultEncoder,
                    $"-b:v:{i}", rate,
                    $"-maxrate:v:{i}", rate,
                    $"-minrate:v:{i}", rate,
                    $"-bufsize:v:{i}", rate,
                    $"-preset:v:{i}", options.EncodePreset,
                    $"-g:v:{i}", "48",
                    $"-sc_threshold:v:{i}", "0",
                    $"-keyint_min:v:{i}", "48"
                ]);

                if (inputAudio != null)
                {
                    ffmpegArguments.AddRange([
                        "-map", "0:a:0",
                        $"-c:a:{i}", "aac",
                        $"-b:a:{i}", options.AudioBitrates[i],
                        $"-ac:a:{i}", "2"
                    ]);
                }
            }

            ffmpegArguments.AddRange([
                "-f", "hls",
                "-hls_time", "10",
                "-hls_playlist_type", "vod",
                "-hls_flags", "independent_segments",
                "-hls_segment_type", "mpegts",
                "-hls_segment_filename", segmentTemplate,
                "-master_pl_name", $"{options.MasterName}.m3u8",
                "-var_stream_map", mapVariants,
                playlistTemplate
            ]);

            await ExecuteCommand(fFMpegOptions.FFMpegPath, ffmpegArguments, callback);
        }

        private async Task<IEnumerable<FFProbeStream>> GetStreams(string inputFile)
        {
            string[] arguments = ["-v", "panic", "-print_format", "json=c=1", "-show_streams", inputFile];
            var value = await ExecuteCommand(fFMpegOptions.FFProbePath, arguments);
            if (string.IsNullOrEmpty(value))
            {
                return [];
            }
            var desirialized = JsonConvert.DeserializeObject<FFProbeObject>(value)!.Streams;
            return desirialized ?? [];
        }

        private async Task<string> ExecuteCommand(string utilPath, IEnumerable<string> arguments, AsyncProgress<double>? onProgressChange = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = utilPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var argument in arguments)
                processStartInfo.ArgumentList.Add(argument);

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = ReadStandardErrorAsync(process.StandardError, onProgressChange);
            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(Math.Max(1, fFMpegOptions.CommandTimeoutSeconds)));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                await TerminateProcessAsync(process);
                throw new TimeoutException(
                    $"Process '{Path.GetFileName(utilPath)}' exceeded the configured timeout of {fFMpegOptions.CommandTimeoutSeconds} seconds.");
            }

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Process '{Path.GetFileName(utilPath)}' failed with exit code {process.ExitCode}: {error}");
            }

            return output;
        }

        private static void ValidateHlsOptions(string input, string output, HlsOptions options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input);
            ArgumentException.ThrowIfNullOrWhiteSpace(output);

            if (options.Resolutions.Count == 0)
                throw new ArgumentException("At least one HLS resolution must be configured.", nameof(options));

            if (options.Bitrates.Count != options.Resolutions.Count ||
                options.AudioBitrates.Count != options.Resolutions.Count)
            {
                throw new ArgumentException(
                    "Resolutions, video bitrates and audio bitrates must contain the same number of values.",
                    nameof(options));
            }

            for (var i = 0; i < options.Resolutions.Count; i++)
            {
                var parts = options.Resolutions[i].Split('x', StringSplitOptions.TrimEntries);
                if (parts.Length != 2 ||
                    !int.TryParse(parts[0], out var width) ||
                    !int.TryParse(parts[1], out var height) ||
                    width <= 0 || height <= 0 || width % 2 != 0 || height % 2 != 0)
                {
                    throw new ArgumentException(
                        $"Resolution '{options.Resolutions[i]}' must have the format WIDTHxHEIGHT and contain positive even dimensions.",
                        nameof(options));
                }

                if (string.IsNullOrWhiteSpace(options.Bitrates[i]) || string.IsNullOrWhiteSpace(options.AudioBitrates[i]))
                    throw new ArgumentException("HLS bitrates cannot be empty.", nameof(options));
            }

            ValidateFileNamePart(options.SegmentFileName, nameof(options.SegmentFileName));
            ValidateFileNamePart(options.MasterName, nameof(options.MasterName));
            ArgumentException.ThrowIfNullOrWhiteSpace(options.EncodePreset);
        }

        private static void ValidateFileNamePart(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                value.Contains('/') || value.Contains('\\') || value.Contains('%'))
            {
                throw new ArgumentException("The value must be a safe file name without path separators or '%' placeholders.", parameterName);
            }
        }

        private async Task<string> ReadStandardErrorAsync(StreamReader reader, AsyncProgress<double>? onProgressChange)
        {
            var error = new StringBuilder();
            while (await reader.ReadLineAsync() is { } line)
            {
                error.AppendLine(line);
                logger.LogDebug("FFmpeg: {Output}", line);

                if (onProgressChange != null && line.StartsWith("frame=", StringComparison.Ordinal))
                {
                    var currentTime = GetCurrentTime(line);
                    await onProgressChange.InvokeAsync(currentTime.TotalSeconds);
                }
            }

            return error.ToString();
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
                // Preserve the timeout as the primary failure if the process cannot be killed.
                return;
            }

            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await process.WaitForExitAsync(waitCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Do not let a stuck child process block the message handler indefinitely.
            }
        }
        private static TimeSpan GetCurrentTime(string data)
        {

            var timeMatch = Regex.Match(data, @"time=(\d{2}):(\d{2}):(\d{2})\.\d+");
            if (!timeMatch.Success)
                return TimeSpan.Zero;

            return new TimeSpan(
                int.Parse(timeMatch.Groups[1].Value),
                int.Parse(timeMatch.Groups[2].Value),
                int.Parse(timeMatch.Groups[3].Value));
        }
    }
}
