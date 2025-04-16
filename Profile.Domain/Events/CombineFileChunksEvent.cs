namespace Blog.Domain.Events
{
    public class CombineFileChunksEvent
    {
        public required Guid EventId { get; set; }
        public required Guid VideoMetadataId { get; set; }
        public required Guid PostId {  get; set; }
        public required bool IsCompleted { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
    }
}
