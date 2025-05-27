using Blog.Domain.Entities;
using MessageBus.EventHandler;
using MessageBus.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.API.Handlers
{
    public class SyncProfileViewsHandler : IEventHandler<UserViewedSyncEvent>
    {
        private readonly IReadWriteRepository<IBlogEntity> _context;

        public SyncProfileViewsHandler(IReadWriteRepository<IBlogEntity> context)
        {
            _context = context;
        }

        public async Task Handle(MessageContext<UserViewedSyncEvent> @event)
        {
            var userId = @event.Message.UserId;
            var ipAddress = @event.Message.RemoteIp;

            var post = await _context.Get<Post>()
                .FirstAsync(x => x.Id == @event.Message.PostId);

            var existView = await _context.Get<PostViewer>()
                .Where(x => x.PostId == post.Id)
                .Where(x => x.UserId == userId || x.UserIpAddress == ipAddress)
                .FirstOrDefaultAsync();

            _context.Attach(post);

            if (existView == null)
            {
                if (@event.Message.IsLike == true)
                {
                    post.LikeCount++;
                }
                if (@event.Message.IsLike == false)
                {
                    post.DislikeCount++;
                }
                if (@event.Message.IsViewed)
                {
                    post.ViewCount++;
                }
                existView = new PostViewer
                {
                    Id = GuidService.GetNewGuid(),
                    PostId = @event.Message.PostId,
                    IsLike = @event.Message.IsLike,
                    UserId = userId,
                    UserIpAddress = ipAddress ?? string.Empty,
                    IsViewed = @event.Message.IsViewed,
                };
                _context.Add(existView);
            }
            else
            {
                if (@event.Message.IsViewed)
                {
                    post.ViewCount++;
                }

                if (@event.Message.IsLike.HasValue)
                {
                    if (@event.Message.IsLike == true)
                    {
                        post.LikeCount = Math.Max(post.LikeCount + (existView?.IsLike == true ? -1 : 1), 0);
                        post.DislikeCount = Math.Max(post.DislikeCount + (existView?.IsLike == false ? -1 : 0), 0);
                    }
                    else
                    {
                        post.LikeCount = Math.Max(post.LikeCount + (existView?.IsLike == true ? -1 : 0), 0);
                        post.DislikeCount = Math.Max(post.DislikeCount + (existView?.IsLike == false ? -1 : 1), 0);
                    }
                }
                else
                {
                    if (existView?.IsLike == true)
                    {
                        post.LikeCount = Math.Max(post.LikeCount - 1, 0);
                    }
                    else if (existView?.IsLike == false)
                    {
                        post.DislikeCount = Math.Max(post.DislikeCount - 1, 0);
                    }
                }

                _context.Attach(existView);
                existView.IsLike = @event.Message.IsLike == existView.IsLike ? null : @event.Message.IsLike;
                existView.UserId = userId;
                existView.UserIpAddress = ipAddress??string.Empty;
                existView.IsViewed = @event.Message.IsViewed;
            }
                await _context.SaveChangesAsync();
        }
    }
}
