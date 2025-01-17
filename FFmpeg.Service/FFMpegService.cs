using FileStorage.Service.Models;
using System.Diagnostics.Tracing;
using System.Diagnostics;
using System.Text;
using Xabe.FFmpeg;
using System.Runtime.ConstrainedExecution;

namespace FFmpeg.Service
{
    /// <summary>
    /// Docs - https://www.nuget.org/packages/FFMpegCore/
    /// </summary>
    public static class FFMpegService
    {
        
        static FFMpegService()
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath("../ffmpeg");
        }


        public static async Task ConvertToH264Async(string input, string filePath, VideoSize size)
        {
            var inputMedia = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(input);
            var inputVideo = inputMedia.VideoStreams.First().SetSize(size).SetCodec(VideoCodec.h264);
            var inputAudio = inputMedia.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac);
            var format = Path.GetExtension(filePath).Replace(".", "");

            var result = await Xabe.FFmpeg.FFmpeg.Conversions.New()
                .AddStream<IVideoStream>(inputVideo)
                .AddStream(inputAudio)
                .SetOutput(filePath)
                .SetOutputFormat(format)
                .Start();
        }

        public static async Task GeneratePreview(string input, string filePath)
        {
            var result = await Xabe.FFmpeg.FFmpeg.Conversions.FromSnippet.Snapshot(input, filePath, TimeSpan.FromSeconds(1));
            await result.Start();
        }

        public static VideoResolution Convert(this VideoSize size)
        {
            switch (size)
            {
                case VideoSize.Hd1080:
                    return VideoResolution.FullHd;
                case VideoSize.Hd720:
                    return VideoResolution.Hd;
                case VideoSize.Vga:
                    return VideoResolution.Middle;
                case VideoSize.Nhd:
                    return VideoResolution.Low;
                default:
                    return VideoResolution.Original;
            }
        }

        public static async Task CreateHls(string input, string output,string fileName = "stream")
        {
            string inputUrl = input;
            string[] resolutions = { "1920x1080", "1280x720", "854x480", "640x360", "256x144" };
            string[] bitrates = { "5M", "3M", "1500k", "1000k", "500k" };
            string[] audioBitrates = { "96k", "96k", "64k", "48k", "48k" };

            StringBuilder filterComplexBuilder = new StringBuilder();
            filterComplexBuilder.Append("[0:v]split=").Append(resolutions.Length);
            for (int i = 1; i <= resolutions.Length; i++)
            {
                filterComplexBuilder.Append($"[v{i}]");
            }

            filterComplexBuilder.Append(";");
            var mapVideoParamsBuilder = new StringBuilder();
            var mapAudioParamsBuilder = new StringBuilder();
            var mapVariantsBuilder = new StringBuilder();


            for (int i = 0; i < resolutions.Length; i++)
            {
                string resolution = resolutions[i];
                string[] parts = resolution.Split('x');
                int width = int.Parse(parts[0]);
                int height = int.Parse(parts[1]);
                string rate = bitrates[i];
                string ab = audioBitrates[i];

                filterComplexBuilder.Append($"[v{i + 1}]scale=w={width}:h={height}[v{i}out];");

                string bufsize = rate.Replace("M", "0M").Replace("k", "k");
                mapVideoParamsBuilder.Append(@$"-map ""[v{i}out]"" -c:v:{i} libx264 -x264-params ""nal - hrd = cbr:force - cfr = 1"" -b:v:{i} {rate} -maxrate:v:{i} {rate} -minrate:v:{i} {rate} -bufsize:v:{i} {bufsize} -preset slow -g 48 -sc_threshold 0 -keyint_min 48 ");

                mapAudioParamsBuilder.Append($"-map 0:a -c:a:{i} aac -b:a:{i} {ab} -ac 2 ");

                mapVariantsBuilder.Append($"v:{i},a:{i} ");
            }

            var filterComplex = filterComplexBuilder.ToString();
            var mapVideoParams = mapVideoParamsBuilder.ToString();
            var mapAudioParams = mapAudioParamsBuilder.ToString();
            var mapVariants = mapVariantsBuilder.ToString().Trim();


            var ffmpegCommand = @$"-i ""{inputUrl}"" -filter_complex ""{filterComplex}"" {mapVideoParams} {mapAudioParams} -f hls -hls_time 2 -hls_playlist_type vod -hls_flags independent_segments -hls_segment_type mpegts -hls_segment_filename {output}/{fileName}_%v/data%05d.ts -master_pl_name {fileName}.m3u8 -var_stream_map ""{mapVariants}"" {output}/{fileName}_%v.m3u8";

            Console.WriteLine($"ffmpeg {ffmpegCommand}");

            await ExecuteCommand(ffmpegCommand);

        }


        private static async Task ExecuteCommand(string command)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "../ffmpeg/ffmpeg.exe",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                
            };

            Process process = new Process { StartInfo = processStartInfo };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"Output: {e.Data}");
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
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            Console.WriteLine($"FFmpeg finished with exit code: {process.ExitCode}");
        }
    }
}
