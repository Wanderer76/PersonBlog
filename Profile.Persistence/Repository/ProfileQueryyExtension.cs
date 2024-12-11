using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;

using Context = Shared.Persistence.IReadWriteRepository<Profile.Domain.Entities.IProfileEntity>;

namespace Profile.Persistence.Repository
{
    public static class ProfileQueryyExtension
    {
        public static async Task<IReadOnlyCollection<AppProfile>> GetAllProfilesPagedAsync(this Context context, int offset, int limit)
        {
            return await context.Get<AppProfile>()
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        public static async Task<AppProfile?> GetProfileByIdAsync(this Context context, Guid id)
        {
            return await context.Get<AppProfile>()
                .Where(x => x.IsDeleted == false)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public static async Task<AppProfile?> GetProfileByUserIdAsync(this Context context, Guid userId)
        {
            return await context.Get<AppProfile>()
                .Where(x => x.IsDeleted == false)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public static async Task<(int TotalPagesCount, IEnumerable<Post> Posts)> GetPostByBlogIdPagedAsync(this Context context, Guid blogId, int page, int limit)
        {
            var totalPostsCount = await context.Get<Post>()
                .Where(x => x.BlogId == blogId)
                .CountAsync();

            var posts = await context.Get<Post>()
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(x => x.FilesMetadata)
                .Include(x => x.VideoFile)
                .ToListAsync();
            var pagesCount = totalPostsCount / limit;
            return (pagesCount == 0 ? 1 : pagesCount, posts);
        }
    }
}
