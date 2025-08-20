using Shared.Services;

namespace Shared.Models
{
    public class UserModel
    {
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string? IpAddress { get; set; }
        public Guid? BlogId { get; set; }
        public bool IsAnonymous => UserId == null;
        public List<Guid> Roles { get; } = [];

        public UserModel(Guid? userId, string userName, string? ipAddress, Guid? blogId, List<Guid> roles)
        {
            UserId = userId;
            UserName = userName;
            IpAddress = ipAddress;
            BlogId = blogId;
            Roles = roles;
        }

        public static UserModel AnonymousUser()
        {
            return new UserModel(null, null, null, null, []);
        }
    }
}
