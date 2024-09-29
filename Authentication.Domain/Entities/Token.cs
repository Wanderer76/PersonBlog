using System.ComponentModel.DataAnnotations.Schema;

namespace Authentication.Domain.Entities;

public class Token : IAuthEntity
{
    public Guid Id { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset ExpiredAt { get; set; } = DateTimeOffset.Now.AddMinutes(15);
    public Guid AppUserId { get; set; }
    
    public string Login { get; set; }

    public Guid RoleId { get; set; }
    public string TokenType { get; set; } = null!;
    
    [ForeignKey(nameof(AppUserId))]
    public AppUser AppUser { get; set; }
}

public class TokenTypes
{
    public const string Access = "access";
    public const string Refresh = "refresh";
}