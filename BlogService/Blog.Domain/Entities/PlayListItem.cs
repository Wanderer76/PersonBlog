using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Blog.Domain.Entities
{
    public class PlayListItem : IBlogEntity
    {
        [JsonInclude]
        public Guid PostId { get; init; }
        [JsonInclude]
        public Guid PlayListId { get; init; }
        [JsonInclude]
        public int Position { get; set; }
        [JsonInclude]
        public DateTimeOffset CreatedAt { get; init; }

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
