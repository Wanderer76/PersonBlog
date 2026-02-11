using Blog.Contracts.Models.Blog;
using Blog.Domain.Entities;
using Infrastructure.Services;

namespace Blog.Service.Models
{
    public static class BlogUserInfoViewMapper
    {
        public static async Task<BlogUserInfoViewModel> ToBlogUserInfoViewModel(this PersonBlog blog, bool hasSubscription, IFileStorage fileStorage)
        {
            return new BlogUserInfoViewModel(blog.Id, blog.Title, blog.Description, blog.CreatedAt, blog.PhotoUrl != null ? await fileStorage.GetFileUrlAsync(blog.Id, blog.PhotoUrl) : null, hasSubscription, blog.SubscriptionsCount);
        }
    }
}
