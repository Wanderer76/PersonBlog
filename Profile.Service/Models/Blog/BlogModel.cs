using Blog.Domain.Entities;

namespace Blog.Service.Models.Blog
{
    public class BlogModel
    {
        public Guid Id { get; }
        public string Name { get; }
        public string? Description { get; }
        public DateTimeOffset CreatedAt { get; }
        public string? PhotoUrl { get; }
        public Guid ProfileId { get; }

        public int SubscribersCount { get; }

        public BlogModel(Guid id, string name, string? description, DateTimeOffset createdAt, string? photoUrl, Guid profileId, int subscribersCount)
        {
            Id = id;
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            PhotoUrl = photoUrl;
            ProfileId = profileId;
            SubscribersCount = subscribersCount;
        }
    }

    public static class BlogModelMapper
    {
        public static BlogModel ToBlogModel(this PersonBlog blog)
        {
            return new BlogModel(blog.Id, blog.Title, blog.Description, blog.CreatedAt, blog.PhotoUrl, blog.ProfileId, blog.SubscriptionsCount);
        }
    }
}
