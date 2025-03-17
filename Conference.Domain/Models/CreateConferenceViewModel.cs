namespace Conference.Domain.Models
{
    public class CreateConferenceViewModel
    {
        public Guid Id { get; }
        public string Url { get; }

        public CreateConferenceViewModel(Guid id, string url)
        {
            Url = url;
            Id = id;
        }
    }
}
