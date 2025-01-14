using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;

using ReadContext = Shared.Persistence.IReadRepository<Profile.Domain.Entities.IProfileEntity>;

namespace Profile.Persistence.Repository
{
    public static class ProfileQueryExtensions
    {
        public static async Task<IReadOnlyCollection<AppProfile>> GetAllProfilesPagedAsync(this ReadContext context, int offset, int limit)
        {
            return await context.Get<AppProfile>()
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        public static async Task<AppProfile?> GetProfileByIdAsync(this ReadContext context, Guid id)
        {
            return await context.Get<AppProfile>()
                .Where(x => x.IsDeleted == false)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public static async Task<AppProfile?> GetProfileByUserIdAsync(this ReadContext context, Guid userId)
        {
            return await context.Get<AppProfile>()
                .Where(x => x.IsDeleted == false)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public static async Task<(int TotalPagesCount, IEnumerable<Post> Posts)> GetPostByBlogIdPagedAsync(this ReadContext context, Guid blogId, int page, int limit)
        {
            var totalPostsCount = await context.Get<Post>()
                .Where(x => x.BlogId == blogId)
                .Where(x=>x.IsDeleted == false)
                .CountAsync();

            var posts = await context.Get<Post>()
                .Include(x => x.VideoFiles)
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var pagesCount = totalPostsCount / limit;
            return (pagesCount == 0 ? 1 : pagesCount, posts);
        }
    }
}
