namespace Conference.Domain.Models
{
    public class ConferenceViewModel
    {
        public Guid Id { get; }
        public string Url { get; }

        public ConferenceViewModel(Guid id, string url)
        {
            Url = url;
            Id = id;
        }
    }
}
