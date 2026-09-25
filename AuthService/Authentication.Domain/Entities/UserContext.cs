
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models;

namespace Authentication.Domain.Entities;
public class UserContext : IAuthEntity
{
    public Guid UserId { get; private set; }
    public string ContextType { get; private set; } = null!;
    public Guid ContextId {  get; private set; }

    [ForeignKey(nameof(UserId))]
    public AppUser AppUser { get; private set; } = null!;

    private UserContext()
    {
        
    }

    public UserContext(Guid userId, string contextType, Guid contextId)
    {
        UserId = userId;
        ContextType = NormalizeContextType(contextType);
        ContextId = contextId;
    }

    [Obsolete("Use the string context type overload.")]
    public UserContext(Guid userId, UserContextType contextType, Guid contextId)
        : this(userId, contextType switch
        {
            UserContextType.Blog => UserContextTypes.Blog,
            UserContextType.Artist => UserContextTypes.Artist,
            _ => throw new ArgumentOutOfRangeException(nameof(contextType), contextType, null)
        }, contextId)
    {
    }

    public static string NormalizeContextType(string contextType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextType);
        return contextType.Trim().ToLowerInvariant();
    }
}

public enum UserContextType
{
    Blog,
    Artist
}
