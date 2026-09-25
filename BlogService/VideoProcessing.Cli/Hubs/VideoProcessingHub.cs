using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VideoProcessing.Cli.Hubs;

[Authorize]
public sealed class VideoProcessingHub : Hub<IVideoProcessHub>
{
    public override async Task OnConnectedAsync()
    {
        var blogId = GetBlogId();
        await Groups.AddToGroupAsync(Context.ConnectionId, GetBlogGroup(blogId));
        await base.OnConnectedAsync();
    }

    internal static string GetBlogGroup(Guid blogId) => $"video-processing:blog:{blogId:N}";

    private Guid GetBlogId()
    {
        var value = Context.User?.FindFirst(AppClaimTypes.BlogId)?.Value;
        if (!Guid.TryParse(value, out var blogId) || blogId == Guid.Empty)
        {
            throw new HubException("An authenticated blog owner is required.");
        }

        return blogId;
    }
}
