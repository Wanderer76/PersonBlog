using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace FFmpeg.Service
{
    /// <summary>
    /// Docs - https://www.nuget.org/packages/FFMpegCore/
    /// </summary>
    public static class FFMpegService
    {
        //TODO вынести в конфиг
        const string FFMpegPath = "../ffmpeg/ffmpeg.exe";
        const string FFProbePath = "../ffmpeg/ffprobe.exe";
        const string DefaultEncoder = "libopenh264"; //h264_nvenc - nvidia, h264_qsv - intel, h264_amf - amd 
        static FFMpegService()
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath("../ffmpeg");
        }


        //public static async Task ConvertToH264Async(string input, string filePath, VideoSize size)
        //{
        //    var inputMedia = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(input);
        //    var inputVideo = inputMedia.VideoStreams.First().SetSize(size).SetCodec(VideoCodec.h264);
        //    var inputAudio = inputMedia.AudioStreams.FirstOrDefault()?.SetCodec(AudioCodec.aac);
        //    var format = Path.GetExtension(filePath).Replace(".", "");

        //    var result = await Xabe.FFmpeg.FFmpeg.Conversions.New()
        //        .AddStream<IVideoStream>(inputVideo)
        //        .AddStream(inputAudio)
        //        .SetOutput(filePath)
        //        .SetOutputFormat(format)
        //        .Start();
        //}

        public static async Task GeneratePreview(string input, string filePath)
        {
            var result = await Xabe.FFmpeg.FFmpeg.Conversions.FromSnippet.Snapshot(input, filePath, TimeSpan.FromSeconds(1));
            await result.Start();
        }

        //public static VideoResolution Convert(this VideoSize size)
        //{
        //    switch (size)
        //    {
        //        case VideoSize.Hd1080:
        //            return VideoResolution.FullHd;
        //        case VideoSize.Hd720:
        //            return VideoResolution.Hd;
        //        case VideoSize.Vga:
        //            return VideoResolution.Middle;
        //        case VideoSize.Nhd:
        //            return VideoResolution.Low;
        //        default:
        //            return VideoResolution.Original;
        //    }
        //}


        public async static Task<FFProbeStream?> GetVideoMediaInfo(string input)
        {
            var inputMedia = await GetStreams(input);
            var inputVideo = inputMedia.FirstOrDefault(x => x.CodecType == "video");
            return inputVideo;
        }

        public static async Task CreateHls(string input, string output,
            string[] resolutions,
            string[] bitrates,
            string[] audioBitrates,
            string fileName, string masterName = "master")
        {
            string inputUrl = input;
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

            var inputMedia = await GetStreams(input);
            var inputAudio = inputMedia.FirstOrDefault(x => x.CodecType == "audio");
            try
            {
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
                    mapVideoParamsBuilder.Append(@$"-map ""[v{i}out]"" -c:v:{i} {DefaultEncoder} -b:v:{i} {rate} -maxrate:v:{i} {rate} -minrate:v:{i} {rate} -bufsize:v:{i} {bufsize} -preset slow -g 48 -sc_threshold 0 -keyint_min 48 -pix_fmt yuv420p ");
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

                var ffmpegCommand = @$"-i ""{inputUrl}"" -filter_complex ""{filterComplex}"" {mapVideoParams} {mapAudioParams} -f hls -hls_time 2 -hls_playlist_type vod -hls_flags independent_segments -hls_segment_type mpegts -hls_segment_filename {output}/{fileName}_%v/data%05d.ts -master_pl_name {masterName}.m3u8 -var_stream_map ""{mapVariants}"" {output}/{fileName}_%v/playlist.m3u8";

                Console.WriteLine($"ffmpeg {ffmpegCommand}");

                await ExecuteCommand(FFMpegPath, ffmpegCommand);
            }
            catch (Exception e)
            {
                Console.WriteLine();
            }
        }

        private static async Task<IEnumerable<FFProbeStream>> GetStreams(string inputFile)
        {
            var value = await ExecuteCommand(FFProbePath, @$"-v panic -print_format json=c=1 -show_streams ""{inputFile}""");
            if (string.IsNullOrEmpty(value))
            {
                return [];
            }
            var desirialized = JsonConvert.DeserializeObject<FFProbeObject>(value).Streams;
            return desirialized ?? [];
        }

        private static async Task<string> ExecuteCommand(string utilPath, string command)
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

            return await process.StandardOutput.ReadToEndAsync();
        }
    }
}
