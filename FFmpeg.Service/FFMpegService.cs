


using Xabe.FFmpeg;

namespace FFmpeg.Service
{
    /// <summary>
    /// Docs - https://www.nuget.org/packages/FFMpegCore/
    /// </summary>
    public static class FFMpegService
    {
        public static async Task ConvertToHlsAsync(Uri input, string filePath)
        {
            Xabe.FFmpeg.FFmpeg.SetExecutablesPath("../ffmpeg");

            var inputMedia = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(input.AbsoluteUri);
            inputMedia.VideoStreams.First().SetSize(VideoSize.Hd720);
            inputMedia.VideoStreams.First().SetCodec(VideoCodec.mpeg4);
            inputMedia.AudioStreams.First().SetCodec(AudioCodec.aac);


            var result = await Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream((IStream)inputMedia.VideoStreams.First(), (IStream)inputMedia.AudioStreams.First())
            .SetOutput(filePath)
            .SetOutputFormat(Path.GetExtension(filePath))
            .SetOverwriteOutput(true)
            .Start();


            // .FromUrlInput(input,options=>
            //options.WithVideoCodec("mpeg4"))
            //    .OutputToUrl((output), options => options
            //    .WithVideoFilters(f => f.Scale(VideoSize.Hd))
            //     // .WithConstantRateFactor(21)
            //      // .WithAudioCodec(AudioCodec.Aac)
            //      //.WithVideoCodec("mpeg4")
            //    .ForceFormat("mp4")
            //    )
            //    .ProcessSynchronously();            // .FromUrlInput(input,options=>
            //options.WithVideoCodec("mpeg4"))
            //    .OutputToUrl((output), options => options
            //    .WithVideoFilters(f => f.Scale(VideoSize.Hd))
            //     // .WithConstantRateFactor(21)
            //      // .WithAudioCodec(AudioCodec.Aac)
            //      //.WithVideoCodec("mpeg4")
            //    .ForceFormat("mp4")
            //    )
            //    .ProcessSynchronously();
        }
    }
}
