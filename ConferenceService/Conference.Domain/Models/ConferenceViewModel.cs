namespace Conference.Domain.Models
{
    public class ConferenceViewModel
    {
        public Guid Id { get; }
        public Guid PostId { get; }
        
        public ConferenceViewModel(Guid id, Guid postId)
        {
            Id = id;
            PostId = postId;
        }
    }
}
