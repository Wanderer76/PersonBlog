using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Blog.Domain.Entities
{
    public class PlayList : IBlogEntity
    {
        [Key]
        [JsonInclude]
        public Guid Id { get; private set; }
        [JsonInclude]
        public Guid BlogId { get; private set; }
        [JsonInclude]
        public string Title { get; set; }
        [JsonInclude]
        public string? ThumbnailId { get; set; }
        [JsonInclude]
        public DateTimeOffset CreatedAt { get; private set; }
        [JsonInclude]
        public bool IsDeleted { get; private set; }
        [JsonIgnore]
        public IReadOnlyList<PlayListItem> PlayListItems => playListItems;

        [JsonInclude]
        private List<PlayListItem> playListItems;

        public PlayList()
        {

        }

        public PlayList(string title, Guid blogId, string? thumbnailId, List<Guid> playListItems)
        {
            Id = GuidService.GetNewGuid();
            Title = title;
            BlogId = blogId;
            ThumbnailId = thumbnailId;
            CreatedAt = DateTimeService.Now();
            IsDeleted = false;
            this.playListItems = playListItems.Select((postId, index) => new PlayListItem(postId, Id, index + 1)).ToList();
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
            var maxPosition = PlayListItems.Max(x => x.Position);
            if (maxPosition + 1 != item.Position)
            {
                return new Error("position bigger than current max +1");
            }
            playListItems.Add(item);
            return true;
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
    }

    public readonly struct PlayListCacheKey(Guid id) : ICacheKey
    {
        private const string Key = nameof(PlayListCacheKey);
        public string GetKey() => $"{Key}:{id}";
    }
}
