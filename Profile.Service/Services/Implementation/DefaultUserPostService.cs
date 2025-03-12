using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Models;
using Profile.Service.Services;
using Shared.Persistence;

namespace Profile.Service.Services.Implementation
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
                var hasView = await _readRepository.Get<PostViewer>()
                    .Where(x => x.PostId == postId)
                    .Where(x => x.UserId == userId || x.UserIpAddress == address)
                    .FirstOrDefaultAsync();

                var hasSub = userId.HasValue && await _readRepository.Get<Subscriber>()
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
