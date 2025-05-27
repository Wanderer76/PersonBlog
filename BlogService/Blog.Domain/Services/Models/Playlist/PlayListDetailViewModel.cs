using Shared.Services;

namespace Blog.Domain.Services.Models.Playlist;

public class PlayListDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string ThumbnailUrl { get; set; }
    public bool CanEdit { get; set; }
    public IEnumerable<PostDetailViewModel> Posts { get; set; }
}

public readonly struct PlayListDetailCacheKey : ICacheKey
{
    private readonly Guid Id;
    private const string Key = nameof(PlayListDetailCacheKey);

    public PlayListDetailCacheKey(Guid id)
    {
        Id = id;
    }

    public string GetKey() => $"{Key}:{Id}";
}