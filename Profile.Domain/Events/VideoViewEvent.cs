using Infrastructure.Models;

namespace Blog.Domain.Events
{
    public class VideoViewEvent
    {
        public required Guid EventId { get; set; }
        public required Guid PostId { get; set; }
        public required DateTimeOffset CreatedAt { get; set; }
        public Guid? UserId { get; set; }
        public string? RemoteIp { get; set; }
    }
}
