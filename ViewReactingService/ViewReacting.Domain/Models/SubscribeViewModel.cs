namespace ViewReacting.Domain.Models
{
    public class SubscribeViewModel
    {
        public Guid BlogId { get; }
        public DateTimeOffset CreatedAt { get; }

        public SubscribeViewModel(Guid blogId, DateTimeOffset createdAt)
        {
            BlogId = blogId;
            CreatedAt = createdAt;
        }

        public override bool Equals(object? obj)
        {
            return obj is SubscribeViewModel other &&
                   BlogId.Equals(other.BlogId) &&
                   CreatedAt.Equals(other.CreatedAt);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BlogId, CreatedAt);
        }
    }
}
