using Blog.Contracts.Events;
using MessageBus.EventHandler;
using MessageBus.Models;

namespace VideoProcessing.Cli.Service;

public sealed class ProcessVideoToHls : IEventHandler<ConvertVideoCommand>
{
    private readonly VideoConversionService _conversionService;

    public ProcessVideoToHls(VideoConversionService conversionService)
    {
        _conversionService = conversionService;
    }

    public async Task Handle(IMessageContext<ConvertVideoCommand> @event)
    {
        var hasPreviewId = @event.Message.HasPreviewId;
        var result = await _conversionService.ProcessConversionAsync(@event.Message, @event.Message.PostId, hasPreviewId);
        await @event.PublishAsync(BaseEvent<VideoConvertedResponse>.Create(result), new() { CorrelationId = result.VideoMetadataId.ToString()});
    }
}
