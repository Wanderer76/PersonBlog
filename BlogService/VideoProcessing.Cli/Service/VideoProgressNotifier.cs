using Microsoft.AspNetCore.SignalR;
using VideoProcessing.Cli.Hubs;

namespace VideoProcessing.Cli.Service;

public interface IVideoProgressNotifier
{
    Task ReportAsync(Guid blogId, Guid postId, double percent, string status, string? error = null);
}

public sealed class SignalRVideoProgressNotifier(
    IHubContext<VideoProcessingHub, IVideoProcessHub> hubContext) : IVideoProgressNotifier
{
    public Task ReportAsync(Guid blogId, Guid postId, double percent, string status, string? error = null)
    {
        var progress = new VideoProcessingProgress(
            postId,
            Math.Clamp(percent, 0, 100),
            status,
            error);

        return hubContext.Clients
            .Group(VideoProcessingHub.GetBlogGroup(blogId))
            .OnVideoConvertProgress(progress);
    }
}
