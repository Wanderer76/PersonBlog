namespace Music.Contract.Events
{
    public class TrackCreateEvent
    {
        public Guid Id { get; }
        public string Title { get; }
        public Guid? AlbumId { get; }
        public Guid? PostId { get; }
        public Guid UploadedByUserId { get; }
        public DateTimeOffset CreatedAt { get; }
    }
}
