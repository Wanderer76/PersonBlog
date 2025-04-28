using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Blog.Domain.Entities
{
    public class PlayListItem : IBlogEntity
    {
        [JsonInclude]
        public Guid PostId { get; }
        [JsonInclude]
        public Guid PlayListId { get; }
        [JsonInclude]
        public int Position { get; set; }
        [JsonInclude]
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
