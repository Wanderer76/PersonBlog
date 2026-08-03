using FFmpeg.Service.Internal;
using FFmpeg.Service.Models;
using FileStorage.Service;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FFmpeg.Service;

public static class FFMpegServiceExtensions
{
    [Obsolete($"Использовать {nameof(AddFFMpegVideoService)}")]
    public static void AddFFMpeg(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddSingleton<IVideoConvertService, FFMpegService>();
        service.AddSingleton<IImageConvertService, FFmpegImageConvertService>();
        service.AddSingleton<FFMpegOptions>(configuration.GetSection("FFMpegOptions:FFMpeg").Get<FFMpegOptions>()!);
        service.AddSingleton<HlsVideoPresets>(configuration.GetSection("FFMpegOptions:HlsVideoPresets").Get<HlsVideoPresets>()!);
    }

    public static void AddFFMpegVideoService(this IServiceCollection service, FFMpegOptions? ffmpegOptions = null, HlsVideoPresets? hlsPresets = null)
    {
        service.AddSingleton<FFMpegOptions>(ffmpegOptions ?? new());
        service.AddSingleton<HlsVideoPresets>(hlsPresets ?? new());
        service.AddSingleton<IVideoConvertService, FFMpegService>();
        service.AddSingleton<IImageConvertService, FFmpegImageConvertService>();
    }

    public static void AddFFMpegAudioExtractorService(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddSingleton<IAudioExtractorService, AudioExtractorFfmpegService>();
        service.AddSingleton<FFMpegOptions>(configuration.GetSection("FFMpegOptions:FFMpeg").Get<FFMpegOptions>()!);
    }
}
