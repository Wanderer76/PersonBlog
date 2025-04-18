using FileStorage.Service.Models;
using Shared.Models;

namespace Blog.Domain.Entities
{
    public sealed class VideoMetadata : FileMetadata, IProfileEntity
    {
        public bool IsProcessed { get; set; }
        public VideoResolution Resolution { get; set; }
        public double Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public ProcessState ProcessState { get; set; }
        public Guid PostId { get; set; }
    }

    public enum ProcessState
    {
        Running,
        Complete,
        Error
    }
}
