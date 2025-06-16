using Blog.Domain.Entities;
using Infrastructure.Interface;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using ReadContext = Shared.Persistence.IReadRepository<Blog.Domain.Entities.IBlogEntity>;

namespace Blog.Persistence.Repository.Quries
{
    public static class ProfileQueryExtensions
    {
        public static async Task<(int TotalPagesCount, int TotalPosts, IEnumerable<Post> Posts)> GetPostByBlogIdPagedAsync(this ReadContext context, Guid blogId, IUserSession userSession, int page, int limit)
        {
            var userId = await context.Get<PersonBlog>()
                .Where(x => x.Id == blogId)
                .Select(x => x.UserId)
                .FirstAsync();

            var currentUser = await userSession.GetUserSessionAsync();

            var canAccessPrivate = currentUser.UserId.HasValue && currentUser.UserId == userId;
            //var paymentLevel = currentUser.UserId.HasValue
            //    ? await context.Get<PaymentSubscriber>()
            //        .Where(x => x.UserId == currentUser.UserId.Value)
            //        .Where(x => x.IsActive == true)
            //        .Select(x => x.SubscriptionLevelId)
            //        .ToListAsync()
            //        : new();

            var totalPostsCount = await context.Get<Post>()
                .Where(x => x.BlogId == blogId)
                .Where(x => x.IsDeleted == false)
                .CountAsync();

            var posts = await context.Get<Post>()
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
                .Where(x => canAccessPrivate || x.Visibility == PostVisibility.Public)
                .OrderByDescending(x => x.CreatedAt)
                .Include(x => x.VideoFile)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var pagesCount = Math.Ceiling(totalPostsCount / (double)limit);
            return (pagesCount == 0 ? 1 : (int)pagesCount, totalPostsCount, posts);
        }

        public static async Task<IEnumerable<VideoProcessEvent>> GetForUpdate(this ProfileDbContext context)
        {
            return await context.ProfileEventMessages
                .FromSqlRaw(
                    @"SELECT * FROM ""Profile"".""ProfileEventMessages""
                        WHERE ""State"" = 0 
                        ORDER BY ""CreatedAt"" 
                        FOR UPDATE SKIP LOCKED 
                        LIMIT 100")
                .AsTracking()
                .ToListAsync();
        }
    }
}
