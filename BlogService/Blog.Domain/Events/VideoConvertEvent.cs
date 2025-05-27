namespace Blog.Domain.Events
{
    public class VideoConvertEvent
    {
        public required Guid EventId { get; set; }
        public required Guid VideoMetadataId { get; set; }
        public string FileUrl { get; set; }
        public Guid UserProfileId { get; set; }
        public required string ObjectName { get; set; }
        public Guid FileId { get; set; }
    }
}
