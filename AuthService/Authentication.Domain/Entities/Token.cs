using Shared;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Authentication.Domain.Entities;

public class Token : IAuthEntity
{
    [Key]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset ExpiredAt { get; set; } = DateTimeOffset.Now.AddMinutes(15);
    public Guid AppUserId { get; set; }

    public string Login { get; set; } = null!;
    public Guid RoleId { get; set; }
    public string TokenType { get; set; } = null!;

    [ForeignKey(nameof(AppUserId))]
    public AppUser AppUser { get; set; } = null!;
}

public static class TokenTypes
{
    public const string Access = "access";
    public const string Refresh = "refresh";
}

public static class TokenExtensions
{
    public static TokenModel ToTokenModel(this Token token, AppUser user)
    {
        var context = user.UserContexts
            .OrderByDescending(x => x.ContextType == Shared.Models.UserContextTypes.Blog)
            .FirstOrDefault();

        return new TokenModel
        {
            Id = token.Id,
            CreatedAt = token.CreatedAt,
            ExpiredAt = token.ExpiredAt,
            Login = token.Login,
            RoleId = token.RoleId,
            UserId = token.AppUserId,
            Type = token.TokenType,
            ContextType = context?.ContextType,
            ContextId = context?.ContextId ?? Guid.Empty
        };
    }
}
