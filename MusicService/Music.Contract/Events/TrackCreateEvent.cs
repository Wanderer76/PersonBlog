using MessageBus;
using System.Text.Json.Serialization;

namespace Music.Contract.Events
{
    [EventPublish(Exchange = "track-events", RoutingKey = "track.create")]
    public class TrackCreateEvent
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid? PostId { get; set; }
        public Guid ArtistId { get; set; }
        public Guid UploadedByUserId { get; set; }
        public List<Guid> Genres { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public TrackCreateEvent()
        {

        }
        [JsonConstructor]
        public TrackCreateEvent(Guid id, string title, Guid? albumId, Guid? postId, Guid artistId, Guid uploadedByUserId, List<Guid> genres, DateTimeOffset createdAt)
        {
            Id = id;
            Title = title;
            AlbumId = albumId;
            PostId = postId;
            ArtistId = artistId;
            UploadedByUserId = uploadedByUserId;
            Genres = genres;
            CreatedAt = createdAt;
        }
    }

    [EventPublish(Exchange = "track-events", RoutingKey = "track.delete")]
    public class TrackDeleteEvent
    {
        public Guid Id { get; }

        public TrackDeleteEvent()
        {

        }

        public TrackDeleteEvent(Guid id)
        {
            Id = id;
        }
    }
}
