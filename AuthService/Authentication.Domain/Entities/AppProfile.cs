﻿using Shared;
using System.ComponentModel.DataAnnotations;

namespace Authentication.Domain.Entities;

public class AppProfile : BaseEntity, IAuthEntity
{
    [Key]
    public Guid  Id { get; set; }
    
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string SurName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string? LastName { get; set; }

    public DateTimeOffset? Birthdate { get; set; }

    public Guid UserId { get; set; }
    public string? PhotoUrl { get; set; }
    public ProfileState ProfileState { get; set; }

    public Guid? BlogId { get; set; }

    //public List<ProfileSubscription> PaymentSubscriptions { get; set; } = [];

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