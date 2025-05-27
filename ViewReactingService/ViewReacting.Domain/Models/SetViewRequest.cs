namespace ViewReacting.Domain.Models
{
    public class SetViewRequest
    {
        public Guid PostId { get; set; }
        public double Time { get; set; }
        public bool IsComplete { get; set; }
    }
}
