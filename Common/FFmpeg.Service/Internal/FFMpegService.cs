using FFmpeg.Service.Models;
using Newtonsoft.Json;
using Shared.Utils;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace FFmpeg.Service.Internal
{
    internal class FFMpegService : IFFMpegService
    {
        private readonly FFMpegOptions fFMpegOptions;
        public FFMpegService(FFMpegOptions configuration)
        {
            fFMpegOptions = configuration;
        }

        public async Task GeneratePreview(string input, string outputFilePath)
        {
            var args = $"-ss {TimeSpan.FromSeconds(1)} -i \"{input}\" -frames:v 1 -q:v 2 \"{outputFilePath}\"";
            await ExecuteCommand(fFMpegOptions.FFMpegPath, args);
        }

        public async Task<FFProbeStream?> GetVideoMediaInfo(string input)
        {
            var inputMedia = await GetStreams(input);
            var inputVideo = inputMedia.FirstOrDefault(x => x.CodecType == "video");
            return inputVideo;
        }

        public async Task CreateHls(string input, string output, HlsOptions options, AsyncProgress<double>? callback)
        {
            options.AssertFound("Опции равны null");
            string inputUrl = input;
            var filterComplexBuilder = new StringBuilder();
            filterComplexBuilder.Append("[0:v]split=").Append(options.Resolutions.Count);
            for (int i = 1; i <= options.Resolutions.Count; i++)
            {
                filterComplexBuilder.Append($"[v{i}]");
            }

            filterComplexBuilder.Append(";");
            var mapVideoParamsBuilder = new StringBuilder();
            var mapAudioParamsBuilder = new StringBuilder();
            var mapVariantsBuilder = new StringBuilder();

            var inputMedia = await GetStreams(input);
            var inputAudio = inputMedia.FirstOrDefault(x => x.CodecType == "audio");
            var inputVideo = inputMedia.FirstOrDefault(x => x.CodecType == "video");

            for (int i = 0; i < options.Resolutions.Count; i++)
            {
                string resolution = options.Resolutions[i];
                string[] parts = resolution.Split('x');
                int width = int.Parse(parts[0]);
                int height = int.Parse(parts[1]);
                string rate = options.Bitrates[i];
                string ab = options.AudioBitrates[i];

                if (inputVideo.Width > inputVideo.Height)
                    filterComplexBuilder.Append($"[v{i + 1}]scale=w={width}:h={height}[v{i}out];");
                else 
                    filterComplexBuilder.Append($"[v{i + 1}]scale=w={height}:h={width}[v{i}out];");


                    string bufsize = rate.Replace("M", "0M").Replace("k", "k");
                mapVideoParamsBuilder.Append($"-map \"[v{i}out]\" -c:v:{i} {fFMpegOptions.DefaultEncoder} " +
                    $"-b:v:{i} {rate} -maxrate:v:{i} {rate} -allow_skip_frames 1 -minrate:v:{i} {rate} -bufsize:v:{i} {bufsize} -preset medium" +
                    $" -g 48 -sc_threshold 0 -keyint_min 48 -pix_fmt yuv420p ");
                if (inputAudio != null)
                {
                    mapAudioParamsBuilder.Append($"-map 0:a -c:a:{i} aac -b:a:{i} {ab} -ac 2 ");
                    mapVariantsBuilder.Append($"v:{i},a:{i} ");
                }
                else
                {
                    mapVariantsBuilder.Append($"v:{i} ");
                }
            }

            var filterComplex = filterComplexBuilder.ToString();
            var mapVideoParams = mapVideoParamsBuilder.ToString();
            var mapAudioParams = mapAudioParamsBuilder.ToString();
            var mapVariants = mapVariantsBuilder.ToString().Trim();

            var ffmpegCommand = @$"-hide_banner -y -i ""{inputUrl}"" -filter_complex ""{filterComplex}"" {mapVideoParams} {mapAudioParams} -f hls -hls_time 10 -hls_playlist_type vod -hls_flags independent_segments -hls_segment_type mpegts -hls_segment_filename {output}/{options.SegmentFileName}_%v/data%05d.ts -master_pl_name {options.MasterName}.m3u8 -var_stream_map ""{mapVariants}"" {output}/{options.SegmentFileName}_%v/playlist.m3u8";

            Console.WriteLine($"ffmpeg {ffmpegCommand}");

            await ExecuteCommand(fFMpegOptions.FFMpegPath, ffmpegCommand, callback);

        }

        private async Task<IEnumerable<FFProbeStream>> GetStreams(string inputFile)
        {
            var value = await ExecuteCommand(fFMpegOptions.FFProbePath, @$"-v panic -print_format json=c=1 -show_streams ""{inputFile}""");
            if (string.IsNullOrEmpty(value))
            {
                return [];
            }
            var desirialized = JsonConvert.DeserializeObject<FFProbeObject>(value)!.Streams;
            return desirialized ?? [];
        }

        private static async Task<string> ExecuteCommand(string utilPath, string command, AsyncProgress<double>? onProgressChange = null)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = utilPath,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Process process = new Process { StartInfo = processStartInfo };

            //process.OutputDataReceived += (sender, args) =>
            //{
            //    if (args.Data != null)
            //    {
            //        try
            //        {
            //            Console.WriteLine(args.Data);
            //            var data = args.Data.Split(" ");
            //            var timeStr = data.FirstOrDefault(x => x.StartsWith("time="));
            //            if (timeStr != null)
            //            {
            //                var time = TimeSpan.Parse(timeStr.Split('=')[1]).TotalSeconds;
            //            }
            //        }catch(Exception e)
            //        {
            //            Console.WriteLine(e);
            //        }
            //    }
            //};

            process.ErrorDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data) && onProgressChange != null)
                {
                    Console.Error.WriteLine($"Error: {e.Data}");
                    if (e.Data.StartsWith("frame="))
                    {
                        var currentTime = GetCurrentTime(e.Data);
                        await onProgressChange.InvokeAsync(currentTime.TotalSeconds);

                    }
                    //var data = e.Data.Split(" ");
                    //var timeStr = data.FirstOrDefault(x => x.StartsWith("time="));
                    //if (timeStr != null && timeStr != "time=N/A")
                    //{
                    //    var time = TimeSpan.Parse(timeStr.Split('=')[1]).TotalSeconds;
                    //}
                }
            };
            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            return await process.StandardOutput.ReadToEndAsync();
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
