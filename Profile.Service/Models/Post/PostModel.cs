using Profile.Domain.Entities;

namespace Profile.Service.Models.Post
{
    public class PostModel
    {
        public Guid Id { get; }
        public PostType Type { get; }
        public string Title { get; }
        public string? Description { get; }
        public DateTimeOffset CreatedAt { get; }
        public string? PreviewId { get; }
        public ProcessState State { get; }
        public VideoMetadataModel? VideoData { get; }
        public string? ErrorMessage { get; }

        public PostModel(Guid id, PostType type, string title, string? description, DateTimeOffset createdAt, string? previewId, VideoMetadataModel? videoData, ProcessState state, string? errorMessage)
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
        }

        public override bool Equals(object? obj)
        {
            return obj is PostModel other &&
                   Id.Equals(other.Id) &&
                   Type == other.Type &&
                   Title == other.Title &&
                   Description == other.Description &&
                   CreatedAt.Equals(other.CreatedAt) &&
                   EqualityComparer<VideoMetadataModel>.Default.Equals(VideoData, other.VideoData);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Type, Title, Description, CreatedAt, VideoData);
        }
    }
}
