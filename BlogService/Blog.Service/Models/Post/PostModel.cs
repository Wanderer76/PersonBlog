using Blog.Domain.Entities;
using Blog.Domain.Services.Models;
using Shared.Services;

namespace Blog.Service.Models.Post
{
    public class PostModel
    {
        public Guid Id { get; }
        public PostType Type { get; }
        public string Title { get; }
        public string? Description { get; }
        public DateTimeOffset CreatedAt { get; }
        public string? PreviewId { get; }
        public int ViewCount { get; }
        public ProcessState State { get; }
        public VideoMetadataModel? VideoData { get; }
        public string? ErrorMessage { get; }

        public PostModel(Guid id, PostType type, string title, string? description, DateTimeOffset createdAt, string? previewId, VideoMetadataModel? videoData, ProcessState state, string? errorMessage, int viewCount)
        {
            Id = id;
            Type = type;
            Title = title;
            Description = description;
            CreatedAt = createdAt;
            PreviewId = previewId;
            VideoData = videoData;
            State = state;
            ErrorMessage = errorMessage;
            ViewCount = viewCount;
        }
    }

    public sealed class PostModelCacheKey : ICacheKey
    {
        private const string Key = nameof(PostModel);
        private readonly Guid id;

        public PostModelCacheKey(Guid id)
        {
            this.id = id;
        }

        public string GetKey() => $"{Key}:{id}";
    }
}
