namespace Profile.Domain.Entities
{
    public class CombineFileChunksEvent : IProfileEntity
    {
        public Guid Id { get; set; }
        public Guid VideoMetadataId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
