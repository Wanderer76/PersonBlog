using Blog.Domain.Entities;
using MassTransit;

namespace Blog.Domain.Events
{

    //[EntityName("video-events")]
    public class VideoReadyToPublishEvent
    {
        public required Guid PostId { get; set; }
        public required Guid VideoMetadataId { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public string? PreviewId { get; set; }
        public string ObjectName { get; set; }
        public double Duration { get; set; }
        public ProcessState ProcessState { get; set; }
    }
}
