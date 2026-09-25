using Shared.Services;

using System.Text.Json.Serialization;

namespace Shared.Models
{
    public class UserModel
    {
        public Guid UserId { get; }
        public string UserName { get; }
        public string? IpAddress { get; }
        public string? ContextType { get; }
        public Guid ContextId { get; }

        /// <summary>Compatibility alias for the active <c>blog</c> context.</summary>
        public Guid BlogId => ContextType == UserContextTypes.Blog ? ContextId : Guid.Empty;

        public bool IsAnonymous => UserId == Guid.Empty;
        public bool HasBlog => ContextType == UserContextTypes.Blog && ContextId != Guid.Empty;
        public List<Guid> Roles { get; } = [];

        public UserModel(Guid userId, string userName, string? ipAddress, Guid blogId, List<Guid> roles)
            : this(
                userId,
                userName,
                ipAddress,
                blogId == Guid.Empty ? null : UserContextTypes.Blog,
                blogId,
                roles)
        {
        }

        [JsonConstructor]
        public UserModel(
            Guid userId,
            string userName,
            string? ipAddress,
            string? contextType,
            Guid contextId,
            List<Guid> roles)
        {
            UserId = userId;
            UserName = userName;
            IpAddress = ipAddress;
            ContextType = contextType;
            ContextId = contextId;
            Roles = roles;
        }

        public static UserModel AnonymousUser()
        {
            return new UserModel(Guid.Empty, null, null, Guid.Empty, []);
        }
    }

    public static class UserContextTypes
    {
        public const string Blog = "blog";
        public const string Artist = "artist";
    }
}
