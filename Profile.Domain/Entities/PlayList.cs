using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;
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
        [JsonInclude]
        public List<PlayListItem> PlayListItems { get; private set; }

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
            PlayListItems = playListItems.Select((postId, index) => new PlayListItem(postId, Id, index + 1)).ToList();
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
            PlayListItems.Add(item);
            return true;
        }
    }

    public readonly struct PlayListCacheKey : ICacheKey
    {
        private readonly Guid Id;
        private const string Key = nameof(PlayListCacheKey);

        public PlayListCacheKey(Guid id)
        {
            Id = id;
        }

        public string GetKey() => $"{Key}:{Id}";
    }
}
