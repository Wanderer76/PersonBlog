using Blog.Domain.Entities;
using Infrastructure.Services;

namespace Blog.Service.Models.Blog;
public static class BlogModelMapper
{
    public static async Task<BlogModel> ToBlogModel(this PersonBlog blog, IFileStorage fileStorage)
    {
        var fileUrl = blog.PhotoUrl == null ? null : await fileStorage.GetFileUrlAsync(blog.Id, blog.PhotoUrl);
        return new BlogModel(blog.Id, blog.Title, blog.Description, blog.CreatedAt, fileUrl, blog.UserId, blog.SubscriptionsCount);
    }
}
