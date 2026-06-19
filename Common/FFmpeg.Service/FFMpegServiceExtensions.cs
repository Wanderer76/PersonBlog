using FFmpeg.Service.Internal;
using FFmpeg.Service.Models;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FFmpeg.Service
{
    public static class FFMpegServiceExtensions
    {
        public static void AddFFMpeg(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddSingleton<IVideoConvertService, FFMpegService>();
            service.AddSingleton<IImageConvertService, FFmpegImageConvertService>();
            service.AddSingleton<FFMpegOptions>(configuration.GetSection("FFMpegOptions:FFMpeg").Get<FFMpegOptions>()!);
            service.AddSingleton<HlsVideoPresets>(configuration.GetSection("FFMpegOptions:HlsVideoPresets").Get<HlsVideoPresets>()!);
        }
        public static void AddFFMpegAudioExtractorService(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddSingleton<IAudioExtractorService, AudioExtractorFfmpegService>();
            service.AddSingleton<FFMpegOptions>(configuration.GetSection("FFMpegOptions:FFMpeg").Get<FFMpegOptions>()!);
        }
    }
}
