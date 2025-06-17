using Shared.Services;

namespace Shared.Models
{
    public class UserModel
    {
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string? IpAddress { get; set; }
        public Guid? BlogId { get; set; }
        public bool IsAnonymous => UserId != null;
        
        public static UserModel AnonymousUser()
        {
            return new UserModel
            {
                UserId = null,
                UserName = null,
                IpAddress = null,
            };
        }
    }
}
