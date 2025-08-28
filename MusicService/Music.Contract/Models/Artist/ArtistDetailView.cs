namespace Music.Contract.Models.Artist;

public class ArtistDetailView
{
    public Guid Id { get; }
    public string Name { get; }
    public string AvatarUrl { get; }
    public int TrackCount { get; }

    public ArtistDetailView(Guid id, string name, string avatarUrl, int trackCount)
    {
        Id = id;
        Name = name;
        AvatarUrl = avatarUrl;
        TrackCount = trackCount;
    }
}
