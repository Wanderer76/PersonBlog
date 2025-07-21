using System.ComponentModel.DataAnnotations;

namespace Comments.Domain.Entities
{
    public class UserProfile : ICommentEntity
    {
        [Key]
        public Guid UserId { get; private set; }
        public string Username { get; set; }
        public string? PhotoUrl { get; set; }

        public UserProfile(Guid userId, string username, string photoUrl)
        {
            UserId = userId;
            Username = username;
            PhotoUrl = photoUrl;
        }
    }
}
