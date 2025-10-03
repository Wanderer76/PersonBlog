namespace Music.Contract.Models
{
    public class GenreListView
    {
        public IReadOnlyList<GenreItem> Items { get; }

        public GenreListView(IReadOnlyList<GenreItem> items)
        {
            Items = items;
        }
    }


    public class GenreItem
    {
        public Guid Id { get; }
        public string Name { get; }

        public GenreItem(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
