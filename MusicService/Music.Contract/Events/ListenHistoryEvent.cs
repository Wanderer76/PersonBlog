using MessageBus;

namespace Music.Contract.Events
{
    [EventPublish(Exchange = "track-listen", RoutingKey = "listen.create")]
    public class ListenHistoryEvent
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TrackId { get; set; }
        public DateTimeOffset ListenedAt { get; set; }

        public ListenHistoryEvent()
        {

        }
        public ListenHistoryEvent(Guid id, Guid userId, Guid trackId, DateTimeOffset listenedAt)
        {
            Id = id;
            UserId = userId;
            TrackId = trackId;
            ListenedAt = listenedAt;
        }
    }
}
