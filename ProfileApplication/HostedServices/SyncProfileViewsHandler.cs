using MessageBus.EventHandler;
using MessageBus.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace ProfileApplication.HostedServices
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
            if (@event.IsLike == true)
            {
                post.LikeCount++;
            }
            if (@event.IsLike == false)
            {
                post.DislikeCount++;
            }
            var existView = await _context.Get<PostViewers>()
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
                post.ViewCount++;
                existView = new PostViewers
                {
                    Id = GuidService.GetNewGuid(),
                    PostId = @event.PostId,
                    IsLike = @event.IsLike,
                    UserId = userId,
                    UserIpAddress = ipAddress,
                    ProfileId = profileId
                };
                _context.Add(existView);
            }
            else
            {
                profileId ??= existView.ProfileId;
                _context.Attach(existView);
                existView.IsLike = @event.IsLike;
                existView.UserId = userId;
                existView.UserIpAddress = ipAddress;
                existView.ProfileId = profileId;
            }

            await _context.SaveChangesAsync();
        }
    }
}
