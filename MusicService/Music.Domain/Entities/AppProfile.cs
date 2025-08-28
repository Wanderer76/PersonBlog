namespace Music.Domain.Entities;

public class AppProfile : IMusicEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; }

    public AppProfile()
    {

    }
    public AppProfile(Guid id, Guid userId, string name)
    {
        Id = id;
        UserId = userId;
        Name = name;
    }
}
