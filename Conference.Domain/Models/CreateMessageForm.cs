namespace Conference.Domain.Models
{
    public class CreateMessageForm
    {
        public Guid ConferenceId { get; set; }
        public string Message { get; set; }
    }
}
