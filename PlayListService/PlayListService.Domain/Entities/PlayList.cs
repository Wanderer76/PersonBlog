using Infrastructure.Interface;
using Shared.Utils;

namespace PlayListService.Domain.Entities;

public sealed class PlayList : IPlayListEntity, ISoftDelete
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }
    //public string? ThumbnailId { get; private set; }
    public Guid? ThumbnailId { get; private set; }
    public Guid UserId { get; private set; }

    public PlayListFile? ThumbnailFile { get; private set; }
    public IReadOnlyList<PlayListItem> PlayListItems => playListItems;

    private readonly List<PlayListItem> playListItems = [];

    private PlayList()
    {

    }

    private PlayList(Guid id, DateTimeOffset createdAt, string title, Guid userId, Guid? thumbnailId, List<Guid> playListItems)
    {
        Id = id;
        Title = title;
        UserId = userId;
        ThumbnailId = thumbnailId;
        CreatedAt = createdAt;
        IsDelete = false;
        this.playListItems = playListItems.Select((postId, index) => new PlayListItem(postId, Id, index + 1, createdAt)).ToList();
    }

    public static Result<PlayList> Create(Guid id, DateTimeOffset createdAt, string title, Guid userId, Guid? thumbnailId, List<Guid> playListItems)
    {
        if (string.IsNullOrEmpty(title))
        {
            return new Error($"{nameof(title)}", "Title is empty");
        }
        if (playListItems.Count != playListItems.Distinct().Count())
        {
            return new Error(nameof(playListItems), "Playlist cannot contain duplicate posts");
        }
        var playList = new PlayList(id, createdAt, title, userId, thumbnailId, playListItems);
        return playList;
    }

    public Result<bool> AddVideo(PlayListItem item)
    {
        if (PlayListItems.Any(x => x.Position == item.Position))
        {
            return new Error("duplicate element");
        }
        if (PlayListItems.Count == 0 && item.Position != 1)
        {
            return new Error("wrong position");
        }
        if (PlayListItems.Any(x => x.PostId == item.PostId))
        {
            return new Error("duplicate element");
        }
        var maxPosition = PlayListItems.Any()
            ? PlayListItems.Max(x => x.Position)
            : 0;
        if (maxPosition + 1 != item.Position)
        {
            return new Error("position bigger than current max +1");
        }
        playListItems.Add(item);
        return true;
    }

    public Result<bool> AddVideo(Guid postId, DateTimeOffset createdAt, int? position = null)
    {
        var destination = 0;
        if (position.HasValue)
        {
            if (playListItems.Any(x => x.Position == position.Value))
            {
                return new Error("400", $"Нельзя добавить на позицию {position}");
            }
            destination = position.Value;
        }
        else
        {
            destination = playListItems.Count != 0 ? playListItems.Max(x => x.Position) + 1 : 1;
        }
        return AddVideo(new PlayListItem(postId, Id, destination, createdAt));
    }

    public Result<bool> RemoveVideo(Guid postId)
    {
        var item = playListItems.FirstOrDefault(x => x.PostId == postId);
        if (item == null) { return new Error("404", "Видео не найдено"); }
        playListItems.Remove(item);
        var position = item.Position;
        var startPosition = 1;
        foreach (var i in PlayListItems.OrderBy(x => x.Position))
        {
            i.Position = startPosition;
            startPosition++;
        }
        return true;
    }

    public Result<bool> ChangeVideoPosition(Guid postId, int destination)
    {
        if (destination < 1 || destination > PlayListItems.Count)
        {
            return new Error(nameof(destination), "Destination is outside the playlist");
        }

        var item = PlayListItems.FirstOrDefault(x => x.PostId == postId);
        if (item == null) { return new Error("404", "Видео не найдено"); }
        var oldPosition = item.Position;
        if (oldPosition == destination)
            return true;

        var direction = oldPosition < destination ? 1 : -1;

        foreach (var other in PlayListItems.Where(i => i.PostId != postId))
        {
            if (direction > 0)
            {
                if (other.Position > oldPosition && other.Position <= destination)
                    other.Position--;
            }
            else
            {
                if (other.Position < oldPosition && other.Position >= destination)
                    other.Position++;
            }
        }
        item.Position = destination;
        return true;
    }

    public void RemovePlayList()
    {
        IsDelete = true;
    }

    public void RestorePlayList()
    {
        IsDelete = false;
    }
}
