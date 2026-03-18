using FileStorage.Service.Models;
using Shared.Models;
using Shared.Services;

namespace Blog.Domain.Entities;

public sealed class VideoFile : BaseFileMetadataEntity, IBlogEntity
{
    public VideoResolution Resolution { get; set; }
    public double Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid PostId { get; set; }
}

public sealed class VideoMetadataCacheKey : ICacheKey
{
    public const string Key = nameof(VideoFile);
    private readonly Guid id;

    public VideoMetadataCacheKey(Guid id)
    {
        this.id = id;
    }

    public string GetKey() => $"{Key}:{id}";
}
