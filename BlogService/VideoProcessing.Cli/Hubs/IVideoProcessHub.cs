namespace VideoProcessing.Cli.Hubs;

public interface IVideoProcessHub
{
    Task OnVideoConvertProgress(VideoProcessingProgress message);
}

public sealed record VideoProcessingProgress(
    Guid PostId,
    double Percent,
    string Status,
    string? Error = null);
