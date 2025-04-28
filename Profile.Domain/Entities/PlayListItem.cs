using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class PlayListItem : IBlogEntity
    {
        public Guid PostId { get; }
        public Guid PlayListId { get; }
        public int Position { get; set; }

        public DateTimeOffset CreatedAt { get; }

        [ForeignKey(nameof(PostId))]
        public Post Post { get; } = null!;

        [ForeignKey(nameof(PlayListId))]
        public PlayList PlayList { get; } = null!;

        public PlayListItem(Guid postId, Guid playListId, int position)
        {
            PostId = postId;
            PlayListId = playListId;
            Position = position;
            CreatedAt = DateTimeService.Now();
        }
    }
}
