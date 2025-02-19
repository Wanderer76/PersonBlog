using FileStorage.Service.Models;

namespace Profile.Domain.Entities
{
    public class VideoMetadata : FileMetadata, IProfileEntity
    {
        public bool IsProcessed { get; set; }
        public VideoResolution Resolution { get; set; }
        public double Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public ProcessState ProcessState { get; set; }
    }

    public enum ProcessState
    {
        Running,
        Complete,
        Error
    }
}
