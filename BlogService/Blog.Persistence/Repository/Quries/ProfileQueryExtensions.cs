using Blog.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using ReadContext = Shared.Persistence.IReadRepository<Blog.Domain.Entities.IBlogEntity>;

namespace Blog.Persistence.Repository.Quries
{
    public static class ProfileQueryExtensions
    {
        public static async Task<(int TotalPagesCount, int TotalPosts, IEnumerable<Post> Posts)> GetPostByBlogIdPagedAsync(this ReadContext context, Guid blogId, ICurrentUserService userSession, int page, int limit)
        {
            var userId = await context.Get<PersonBlog>()
                .Where(x => x.Id == blogId)
                .Select(x => x.UserId)
                .FirstAsync();

            var currentUser = await userSession.GetCurrentUserAsync();

            var canAccessPrivate = !currentUser.IsAnonymous && currentUser.UserId == userId;
            //var paymentLevel = currentUser.UserId.HasValue
            //    ? await context.Get<PaymentSubscriber>()
            //        .Where(x => x.UserId == currentUser.UserId.Value)
            //        .Where(x => x.IsActive == true)
            //        .Select(x => x.SubscriptionLevelId)
            //        .ToListAsync()
            //        : new();

            var postQuery = context.Get<Post>()
                .Where(x => x.BlogId == blogId)
                .Where(x => x.IsDelete == false);

            if (!canAccessPrivate)
            {
                postQuery = postQuery.Where(x => x.BanMessageId == null);
            }

            var totalPostsCount = await postQuery.CountAsync();


            var posts = await postQuery
                .Where(x => canAccessPrivate || x.Visibility == PostVisibility.Public)
                .OrderByDescending(x => x.CreatedAt)
                .Include(x => x.VideoPostInfo)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var pagesCount = Math.Ceiling(totalPostsCount / (double)limit);
            return (pagesCount == 0 ? 1 : (int)pagesCount, totalPostsCount, posts);
        }

        public static async Task<IEnumerable<VideoProcessEvent>> GetForUpdate(this BlogDbContext context)
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
