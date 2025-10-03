namespace Music.Contract.Models.Search
{
    public class SearchFilter
    {
        public SortOrder? Order { get; set; }
        public string? Title { get; set; }
        public List<Guid>? Genres { get; set; }
        public List<Guid>? Ids { get; set; }
        public SearchFilter()
        {
            Genres = [];
            Ids = [];
        }
    }

    public enum SortOrder
    {
        New,
        Popular,
    }
}
