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
        private readonly IReadWriteRepository<IProfileEntity> _context;

        public SyncProfileViewsHandler(IReadWriteRepository<IProfileEntity> context)
        {
            _context = context;
        }

        public async Task Handle(UserViewedSyncEvent @event)
        {
            var userId = @event.UserId;
            var ipAddress = @event.RemoteIp;

            var post = await _context.Get<Post>()
                .FirstAsync(x => x.Id == @event.PostId);

            var existView = await _context.Get<PostViewer>()
                .Where(x => x.PostId == post.Id)
                .Where(x => x.UserId == userId || x.UserIpAddress == ipAddress)
                .FirstOrDefaultAsync();

            _context.Attach(post);

            Guid? profileId = null;
            if (userId != null)
            {
                profileId = await _context.Get<AppProfile>()
                    .Where(x => x.UserId == userId)
                    .Select(x => x.Id)
                    .FirstAsync();
            }
            if (existView == null)
            {
                if (@event.IsLike == true)
                {
                    post.LikeCount++;
                }
                if (@event.IsLike == false)
                {
                    post.DislikeCount++;
                }
                if (@event.IsViewed)
                {
                    post.ViewCount++;
                }
                existView = new PostViewer
                {
                    Id = GuidService.GetNewGuid(),
                    PostId = @event.PostId,
                    IsLike = @event.IsLike,
                    UserId = userId,
                    UserIpAddress = ipAddress,
                    ProfileId = profileId,
                    IsViewed = @event.IsViewed,
                };
                _context.Add(existView);
            }
            else
            {
                if (@event.IsViewed)
                {
                    post.ViewCount++;
                }

                if (@event.IsLike.HasValue)
                {
                    if (@event.IsLike == true)
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

                profileId ??= existView.ProfileId;
                _context.Attach(existView);
                existView.IsLike = @event.IsLike == existView.IsLike ? null : @event.IsLike;
                existView.UserId = userId;
                existView.UserIpAddress = ipAddress;
                existView.ProfileId = profileId;
                existView.IsViewed = @event.IsViewed;
            }

            await _context.SaveChangesAsync();
        }
    }
}
