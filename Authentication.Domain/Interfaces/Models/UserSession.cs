namespace Authentication.Domain.Interfaces.Models
{
    public class UserSession
    {
        public Guid? UserId { get; set; }
        public string? IpAddress { get; set; }
        public bool IsAnonymous => UserId != null;
    }
}
