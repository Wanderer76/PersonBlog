using Shared;
using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities;

public class AppProfile : BaseEntity<Guid>, IProfileEntity
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string SurName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public DateTimeOffset? Birthdate { get; set; }

    public Guid UserId { get; set; }
    public string? PhotoUrl { get; set; }
    public ProfileState ProfileState { get; set; }

    public List<ProfileSubscription> PaymentSubscriptions { get; set; } = [];

    public AppProfile() { }

    internal AppProfile(DateTimeOffset? birthdate, string email, string firstName, string surName, string? lastName, Guid userId)
    {
        Id = userId;
        Birthdate = birthdate;
        Email = email;
        FirstName = firstName;
        SurName = surName;
        LastName = lastName;
        UserId = userId;
        IsDeleted = false;
        ProfileState = ProfileState.Active;

    }

    public static AppProfile Create(DateTimeOffset? birthdate, string email, string firstName, string surName, string? lastName, Guid userId)
    {
        return new AppProfile(birthdate, email, firstName, surName, lastName, userId);
    }
}