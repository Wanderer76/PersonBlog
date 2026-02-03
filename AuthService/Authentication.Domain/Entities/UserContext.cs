
using System.ComponentModel.DataAnnotations.Schema;

namespace Authentication.Domain.Entities;
public class UserContext : IAuthEntity
{
    public Guid UserId { get; private set; }
    public UserContextType ContextType { get; private set; }
    public Guid ContextId {  get; private set; }

    [ForeignKey(nameof(UserId))]
    public AppUser AppUser { get; private set; } = null!;

    private UserContext()
    {
        
    }

    public UserContext(Guid userId, UserContextType contextType,Guid contextId)
    {
        UserId = userId;
        ContextType = contextType;
        ContextId = contextId;
    }
}

public enum UserContextType
{
    Blog,
    Artist
}
