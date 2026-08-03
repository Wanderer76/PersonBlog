using Blog.Contracts.Events;
using FFmpeg.Service.Models;
using MessageBus;
using MessageBus.EventHandler;
using MessageBus.Models;
using Shared.Models;
using Infrastructure.Services;
using FileStorage.Service;

namespace VideoProcessing.Cli.Service;

public sealed class ProcessVideoToHls : IEventHandler<ConvertVideoCommand>
{
    private readonly VideoConversionService _conversionService;

    public ProcessVideoToHls(IVideoConvertService ffmpegService, IFileStorageFactory storage, IConfiguration configuration, HlsVideoPresets videoPresets)
    {
        _conversionService = new VideoConversionService(ffmpegService, storage, configuration, videoPresets);
    }

    public async Task Handle(IMessageContext<ConvertVideoCommand> @event)
    {
        var hasPreviewId = @event.Message.HasPreviewId;
        var result = await _conversionService.ProcessConversionAsync(@event.Message, @event.Message.PostId, hasPreviewId);
        await @event.PublishAsync(BaseEvent<VideoConvertedResponse>.Create(result), new() { CorrelationId = result.VideoMetadataId.ToString()});
    }
}
