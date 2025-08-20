namespace Profile.Domain.Models;

public class PostReport
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; }
    public Guid ReasonId { get; set; }
    public string ObjectName { get; set; }
}