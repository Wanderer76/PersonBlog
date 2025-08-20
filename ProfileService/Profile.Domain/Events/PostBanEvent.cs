using MessageBus;

namespace Profile.Domain.Events
{
    [EventPublish(Exchange = "admin-sync", RoutingKey = "post.ban")]
    public class PostBanEvent
    {
        public Guid PostId { get; set; }
        public string ObjectName { get; set; }
        public Guid ReasonId { get; set; }
        public string UserMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatorUserId { get; set; }
    }
}
