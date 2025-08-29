namespace Music.Contract.Models.PlayList
{
    public class CreatePlayListRequest
    {
        public string Title { get; set; }
        public List<Guid> Tracks { get; set; }
    }
}
