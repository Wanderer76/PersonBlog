using Blog.Domain.Entities;

namespace Blog.Domain.Events
{

    public class VideoReadyToPublishEvent
    {
        public required Guid PostId { get; set; }
        public required Guid VideoMetadataId { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public string? PreviewId { get; set; }
        public string ObjectName { get; set; }
        public double Duration { get; set; }
        public ProcessState ProcessState { get; set; }
        public string? Error {  get; set; }
    }
}
