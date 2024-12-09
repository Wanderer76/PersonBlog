using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class Post : IProfileEntity
    {
        [Key]
        public Guid Id { get; private set; }

        public Guid BlogId { get; private set; }

        public PostType Type { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public string? Text { get; set; }
        public Guid? FileId { get; private set; }

        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(BlogId))]
        public Blog Blog { get; set; }
        
        [ForeignKey(nameof(FileId))]
        public VideoMetadata? VideoMetadata { get; private set; }

        public Post(Guid id, Guid blogId, PostType type, DateTimeOffset createdAt, string? text, Guid? fileId, bool isDeleted)
        {
            Id = id;
            BlogId = blogId;
            Type = type;
            CreatedAt = createdAt;
            Text = text;
            FileId = fileId;
            IsDeleted = isDeleted;
        }
    }

    public enum PostType
    {
        Text,
        Media
    }
}
