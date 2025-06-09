namespace Shared.Models
{
    public class UserSession
    {
        public Guid SessionId { get; set; }
        public Guid? UserId { get; set; }

        public string UserName { get; set; }
        public string? IpAddress { get; set; }
        public Guid BlogId { get; set; }
        public bool IsAnonymous => UserId != null;
        
        public static UserSession AnonymousUser()
        {
            return new UserSession
            {
                SessionId = Guid.Empty,
                UserId = null,
                UserName = null,
                IpAddress = null,
            };
        }
    }
}
