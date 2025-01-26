using System.ComponentModel.DataAnnotations;

namespace Video.Domain.Entities
{
    public class UserViewedPost : IVideoEntity
    {
        [Key]
        public Guid Id { get; }
        public Guid UserId { get; }
        public Guid PostId { get; }
        public DateTimeOffset ViewedAt { get; }

        public UserViewedPost(Guid id, Guid userId, Guid postId, DateTimeOffset viewedAt)
        {
            Id = id;
            UserId = userId;
            PostId = postId;
            ViewedAt = viewedAt;
        }
    }
}
