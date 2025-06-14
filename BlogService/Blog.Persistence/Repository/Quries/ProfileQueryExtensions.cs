﻿using Blog.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using ReadContext = Shared.Persistence.IReadRepository<Blog.Domain.Entities.IBlogEntity>;

namespace Blog.Persistence.Repository.Quries
{
    public static class ProfileQueryExtensions
    {
        public static async Task<(int TotalPagesCount, int TotalPosts, IEnumerable<Post> Posts)> GetPostByBlogIdPagedAsync(this ReadContext context, Guid blogId, int page, int limit)
        {
            var totalPostsCount = await context.Get<Post>()
                .Where(x => x.BlogId == blogId)
                .Where(x => x.IsDeleted == false)
                .CountAsync();

            var posts = await context.Get<Post>()
                .Where(x => x.BlogId == blogId && x.IsDeleted == false)
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
