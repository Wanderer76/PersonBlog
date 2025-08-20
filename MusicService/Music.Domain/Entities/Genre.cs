namespace Music.Domain.Entities
{
    public class Genre : IMusicEntity
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public Genre(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
