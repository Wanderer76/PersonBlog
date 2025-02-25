using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Models;
using Shared.Persistence;

namespace Profile.Service.Interface.Implementation
{
    internal class DefaultUserPostService : IUserPostService
    {
        private readonly IReadRepository<IProfileEntity> _readRepository;

        public DefaultUserPostService(IReadRepository<IProfileEntity> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<UserViewInfo> GetUserViewPostInfoAsync(Guid postId, Guid? userId, string? address)
        {
            if (userId.HasValue || address != null)
            {
                var hasView = await _readRepository.Get<PostViewers>()
                    .Where(x => x.PostId == postId)
                    .Where(x => x.UserId == userId || x.UserIpAddress == address)
                    .FirstOrDefaultAsync();

                var hasSub = userId.HasValue && await _readRepository.Get<Subscription>()
                    .Active()
                    .ByUserId(userId.Value)
                    .AnyAsync();

                return new UserViewInfo(hasView != null, hasView?.IsLike, hasSub);
            }
            else
            {
                return UserViewInfo.CreateEmpty;
            }
        }
    }
}
