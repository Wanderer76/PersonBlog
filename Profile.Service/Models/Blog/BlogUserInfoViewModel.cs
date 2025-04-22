using Blog.Domain.Entities;

namespace Blog.Service.Models.Blog
{
    public class BlogUserInfoViewModel
    {
        public Guid Id { get; }
        public string Name { get; }
        public string? Description { get; }
        public DateTimeOffset CreatedAt { get; }
        public string? PhotoUrl { get; }
        public bool HasSubscription { get; }
        public int SubscribersCount { get; }

        public BlogUserInfoViewModel(Guid id, string name, string? description, DateTimeOffset createdAt, string? photoUrl, bool hasSubscription, int subscribersCount)
        {
            Id = id;
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            PhotoUrl = photoUrl;
            HasSubscription = hasSubscription;
            SubscribersCount = subscribersCount;
        }
    }
    public static class BlogUserInfoViewMapper
    {
        public static BlogUserInfoViewModel ToBlogUserInfoViewModel(this PersonBlog blog, bool hasSubscription)
        {
            return new BlogUserInfoViewModel(blog.Id, blog.Title, blog.Description, blog.CreatedAt, blog.PhotoUrl, hasSubscription, blog.SubscriptionsCount);
        }
    }
}
