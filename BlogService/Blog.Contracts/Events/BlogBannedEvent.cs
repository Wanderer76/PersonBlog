namespace Blog.Contracts.Events
{
    public class BlogBannedEvent
    {
        public Guid BlogId {  get; set; }
        public string Message {  get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
