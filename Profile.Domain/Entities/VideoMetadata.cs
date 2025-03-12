using FileStorage.Service.Models;
using Shared.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public sealed class VideoMetadata : FileMetadata, IProfileEntity
    {
        public bool IsProcessed { get; set; }
        public VideoResolution Resolution { get; set; }
        public double Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public ProcessState ProcessState { get; set; }
        public Guid PostId { get; set; }

        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; } = null!;
    }

    public enum ProcessState
    {
        Running,
        Complete,
        Error
    }
}
