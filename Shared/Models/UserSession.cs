namespace Shared.Models
{
    public class UserSession
    {
        public Guid SessionId { get; set; }
        public Guid? UserId { get; set; }
        public string? IpAddress { get; set; }
        public bool IsAnonymous => UserId != null;
    }
}
