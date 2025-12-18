using Shared.Services;

namespace Shared.Models
{
    public class UserModel
    {
        public Guid UserId { get; }
        public string UserName { get; }
        public string? IpAddress { get; }
        public Guid BlogId { get; }
        public bool IsAnonymous => UserId == Guid.Empty;
        public bool HasBlog => BlogId == Guid.Empty;
        public List<Guid> Roles { get; } = [];

        public UserModel(Guid userId, string userName, string? ipAddress, Guid blogId, List<Guid> roles)
        {
            UserId = userId;
            UserName = userName;
            IpAddress = ipAddress;
            BlogId = blogId;
            Roles = roles;
        }

        public static UserModel AnonymousUser()
        {
            return new UserModel(Guid.Empty, null, null, Guid.Empty, []);
        }
    }
}
