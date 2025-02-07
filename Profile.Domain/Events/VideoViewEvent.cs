using Infrastructure.Models;

namespace Profile.Domain.Events
{
    public class VideoViewEvent
    {
        public required Guid PostId { get; set; }
        public required DateTimeOffset CreatedAt {  get; set; }
        public Guid? UserId { get; set; }
        public string? RemoteIp { get; set; }
    }
}
