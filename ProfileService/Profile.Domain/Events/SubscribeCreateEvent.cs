using MessageBus;

namespace Profile.Domain.Events
{
    [EventPublish(Exchange = "user-subscribe", RoutingKey = "created")]
    public class SubscribeCreateEvent
    {
        public Guid BlogId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    [EventPublish(Exchange = "user-subscribe", RoutingKey = "canceled")]
    public class SubscribeCancelEvent
    {
        public Guid BlogId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
