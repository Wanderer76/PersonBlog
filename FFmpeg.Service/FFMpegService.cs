using FileStorage.Service.Models;
using Xabe.FFmpeg;

namespace FFmpeg.Service
{
    /// <summary>
    /// Docs - https://www.nuget.org/packages/FFMpegCore/
    /// </summary>
    public static class FFMpegService
    {
        public static async Task ConvertToH264Async(string input, string filePath, VideoSize size)
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath("../ffmpeg");

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
    }
}
