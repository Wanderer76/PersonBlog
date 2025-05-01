using Blog.Domain.Entities;
using Blog.Service.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultUserPostService : IUserPostService
    {
        private readonly IReadRepository<IBlogEntity> _readRepository;

        public DefaultUserPostService(IReadRepository<IBlogEntity> readRepository)
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
