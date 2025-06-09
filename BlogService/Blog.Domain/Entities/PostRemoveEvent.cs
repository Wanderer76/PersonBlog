using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class PostRemoveEvent : IBlogEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; private set; }
        public Guid PostId { get; private set; }
        public DateTimeOffset DeletedAt {  get; private set; }

        public PostRemoveEvent(Guid postId, DateTimeOffset deletedAt)
        {
            PostId = postId;
            DeletedAt = deletedAt;
        }
    }
}
