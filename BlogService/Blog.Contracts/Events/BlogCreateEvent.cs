using MessageBus;

namespace Blog.Contracts.Events
{
    [EventPublish(Exchange = "blogs", RoutingKey = "blog.create")]
    public class BlogCreateEvent 
    {
        public Guid BlogId { get; private set; }
        public Guid UserId { get; private set; }

        public BlogCreateEvent(Guid blogId, Guid userId)
        {
            BlogId = blogId;
            UserId = userId;
        }
    }
}
