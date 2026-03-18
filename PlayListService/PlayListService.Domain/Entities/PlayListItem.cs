using Infrastructure.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlayListService.Domain.Entities;

public sealed class PlayListItem : IPlayListEntity, ISoftDelete
{
    public Guid PostId { get; init; }
    public Guid PlayListId { get; init; }
    public int Position { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    [ForeignKey(nameof(PlayListId))]
    public PlayList PlayList { get; } = null!;

    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }

    public PlayListItem(Guid postId, Guid playListId, int position, DateTimeOffset createdAt)
    {
        PostId = postId;
        PlayListId = playListId;
        Position = position;
        CreatedAt = createdAt;
        IsDelete = false;
        DeleteDateTime = null;
    }
}
