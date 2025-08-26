namespace Music.Contract.Models;

public class ArtistInfo
{
    public Guid? Id { get; }
    public string Name { get; }

    public ArtistInfo(Guid? id, string name)
    {
        Id = id;
        Name = name;
    }
}
