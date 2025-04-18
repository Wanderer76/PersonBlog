using Blog.Domain.Entities;
using Shared.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
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

     

        public string? PreviewId { get; set; }
        public Guid? VideoFileId { get; set; }
        public Guid? PaymentSubscriptionId { get; set; }

        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }

        public PostVisibility Visibility { get; set; }


        [ForeignKey(nameof(VideoFileId))]
        public VideoMetadata? VideoFile { get; set; }

        [ForeignKey(nameof(BlogId))]
        public PersonBlog Blog { get; set; }

        private Post() { }

        public Post(Guid id, Guid blogId, PostType type, string? description, string title, Guid? paymentSubscriptionId, PostVisibility visibility)
        {
            Id = id;
            BlogId = blogId;
            Type = type;
            CreatedAt = DateTimeService.Now();
            Description = description;
            IsDeleted = false;
            Title = title;
            PaymentSubscriptionId = paymentSubscriptionId;
            Visibility = visibility;
        }
    }

    public enum PostType
    {
        Text,
        Video
    }
}
