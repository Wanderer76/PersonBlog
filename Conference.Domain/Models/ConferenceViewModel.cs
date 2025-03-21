namespace Conference.Domain.Models
{
    public class ConferenceViewModel
    {
        public Guid Id { get; }
        public string Url { get; }
        public Guid PostId { get; }
        
        public ConferenceViewModel(Guid id, string url, Guid postId)
        {
            Url = url;
            Id = id;
            PostId = postId;
        }
    }
}
