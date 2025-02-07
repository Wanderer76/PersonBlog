namespace Profile.Domain.Events
{
    public class CombineFileChunksEvent
    {
        public Guid Id { get; set; }
        public Guid VideoMetadataId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
