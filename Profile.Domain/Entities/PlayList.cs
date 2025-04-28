using Shared.Services;
using Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities
{
    public class PlayList : IBlogEntity
    {
        [Key]
        public Guid Id { get; }
        public Guid BlogId { get; }
        public string Title { get; set; }
        public string? ThumbnailId { get; set; }
        public DateTimeOffset CreatedAt { get; }
        public bool IsDeleted { get; private set; }
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
                return Result<bool>.Failure(new("duplicate element"));
            }
            if (PlayListItems.Count == 0 && item.Position != 1)
            {
                return Result<bool>.Failure(new("wrong position"));
            }
            if (PlayListItems.Any(x => x.PostId == item.PostId))
            {
                return Result<bool>.Failure(new("duplicate element"));
            }
            PlayListItems.Add(item);
            return Result<bool>.Success(true);
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
