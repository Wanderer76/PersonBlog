using Shared;
using Shared.Services;
using System.ComponentModel.DataAnnotations;

namespace Profile.Domain.Entities;

public class AppProfile : BaseEntity, IUserEntity
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; }

    //[Required]
    //[EmailAddress]
    //public string Email { get; set; }

    //public DateTimeOffset? Birthdate { get; set; }

    public Guid UserId { get; set; }
    public string? PhotoUrl { get; set; }
    public ProfileState ProfileState { get; set; }

    public Guid? BlogId { get; set; }

    //public List<ProfileSubscription> PaymentSubscriptions { get; set; } = [];

    public AppProfile() { }

    internal AppProfile(string name, Guid userId)
    {
        UserId = userId;
        IsDeleted = false;
        Name = string.IsNullOrEmpty(name) ? "anon" : name;
        ProfileState = ProfileState.Active;

    }

    public static AppProfile Create(string name, Guid userId)
    {
        return new AppProfile(name, userId);
    }
}

public class AppProfileCacheKey : ICacheKey
{
    private const string Key = nameof(AppProfile);
    private readonly Guid userId;

    public AppProfileCacheKey(Guid userId)
    {
        this.userId = userId;
    }

    public string GetKey() => $"{Key}:{userId}";
}
