using FileStorage.Service.Models;
using Shared.Models;
using Shared.Services;

namespace Blog.Domain.Entities
{
    public sealed class VideoMetadata : FileMetadata, IBlogEntity
    {
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
        Load,
        Error
    }

    public static class ProcessStateExtensions
    {
        public static bool IsComplete(this ProcessState state) {  return state == ProcessState.Complete; }
    }

    public sealed class VideoMetadataCacheKey : ICacheKey
    {
        public const string Key = nameof(VideoMetadata);
        private readonly Guid id;

        public VideoMetadataCacheKey(Guid id)
        {
            this.id = id;
        }

        public string GetKey() => $"{Key}:{id}";
    }
}
