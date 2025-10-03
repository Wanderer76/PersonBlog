using Music.Contract.Models.Artist;

namespace Music.Contract.Models;

public class TrackViewItem
{
    public Guid Id { get; }
    public string Name { get; }
    public string? ThumbnailUrl { get; }
    public Guid? AlbumId { get; }
    public bool IsLiked { get; }
    public TrackFileInfo TrackInfo { get; }
    public IReadOnlyList<ArtistInfo> Artists { get; }

    public TrackViewItem(Guid id, string name, string? thumbnailUrl, Guid? albumId, TrackFileInfo trackInfo, IReadOnlyList<ArtistInfo> artists, bool isLiked)
    {
        Id = id;
        Name = name;
        ThumbnailUrl = thumbnailUrl;
        AlbumId = albumId;
        TrackInfo = trackInfo;
        Artists = artists;
        IsLiked = isLiked;
    }

}

