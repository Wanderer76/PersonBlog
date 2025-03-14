namespace Blog.Domain.Events
{
    public class CombineFileChunksEvent
    {
        public Guid EventId { get; set; }
        public Guid VideoMetadataId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
