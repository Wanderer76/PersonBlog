using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class Post : IProfileEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public Guid BlogId { get; private set; }
        public PostType Type { get; set; }
        public DateTimeOffset CreatedAt { get; private set; }

        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(BlogId))]
        public Blog Blog { get; set; }

        public string? PreviewId { get; set; }

        public VideoMetadata VideoFile { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }

        private Post() { }

        public Post(Guid id, Guid blogId, PostType type, DateTimeOffset createdAt, string? description, bool isDeleted, string title)
        {
            Id = id;
            BlogId = blogId;
            Type = type;
            CreatedAt = createdAt;
            Description = description;
            IsDeleted = isDeleted;
            Title = title;
        }
    }

    public enum PostType
    {
        Text,
        Video
    }
}
