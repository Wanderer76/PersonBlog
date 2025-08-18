using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities
{
    public class BanMessage : IBlogEntity
    {
        [Key]
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public DateTimeOffset? BannedDateTime { get; private set; }
        public string? BanReason { get; private set; }
        public Post Post { get; private set; }

        public BanMessage(Guid id, Guid postId, DateTimeOffset? bannedDateTime, string? banReason)
        {
            Id = id;
            PostId = postId;
            BannedDateTime = bannedDateTime;
            BanReason = banReason;
        }
    }
}
