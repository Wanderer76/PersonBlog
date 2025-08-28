namespace Music.Contract.Models.Search
{
    public class SearchFilter
    {
        public SortOrder? Order { get; set; }
        public string? Title { get; set; }
        public List<Guid>? Genres { get; set; }
        public SearchFilter()
        {
            Genres = [];
        }
    }

    public enum SortOrder
    {
        New,
        Popular,
    }
}
