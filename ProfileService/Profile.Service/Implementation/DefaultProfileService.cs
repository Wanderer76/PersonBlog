using Authentication.Contract.Events;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Domain.Models.Profile;
using Profile.Domain.Services;
using Shared.Persistence;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Profile.Test")]

namespace Profile.Service.Implementation;

internal class DefaultProfileService : IProfileService
{
    private readonly IReadWriteRepository<IUserEntity> _context;

    public DefaultProfileService(IReadWriteRepository<IUserEntity> profileRepository)
    {
        _context = profileRepository;
    }

    public async Task<ProfileModel> CreateProfileAsync(ProfileRegisterEvent profileCreateModel)
    {
        var isProfileAlreadyExist = await _context.Get<AppProfile>()
            .AnyAsync(x => x.UserId == profileCreateModel.UserId && x.IsDeleted == true);
        if (isProfileAlreadyExist)
        {
            throw new ArgumentException("Пользователь уже существует");
        }

        var profile = AppProfile.Create(
            birthdate: profileCreateModel.Birthdate,
            email: profileCreateModel.Email,
            name: profileCreateModel.Name,
            userId: profileCreateModel.UserId
        );
        _context.Add(profile);
        await _context.SaveChangesAsync();
        return profile.ToProfileModel();
    }

    public async Task DeleteProfileByUserIdAsync(Guid userId)
    {
        var profile = await _context.Get<AppProfile>()
            .FirstAsync(x => x.UserId == userId && x.IsDeleted == false);

        _context.Attach(profile);
        profile.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<ProfileModel> GetProfileByUserIdAsync(Guid userId)
    {
        var profile = await _context.Get<AppProfile>()
            .FirstAsync(x => x.UserId == userId);

        return profile.ToProfileModel();
    }

    public async Task<Guid?> GetProfileIdByUserIdIfExistsAsync(Guid userId)
    {
        var profileId = await _context.Get<AppProfile>()
            .Where(x => x.UserId == userId)
            .Select(x => x.Id)
            .Cast<Guid?>()
            .FirstOrDefaultAsync();

        return profileId;
    }
    public async Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel)
    {
        var profile = await _context.Get<AppProfile>()
            .FirstAsync(x => x.Id == profileEditModel.Id);

        _context.Attach(profile);
        profile.Birthdate = profileEditModel.Birthdate;
        profile.Email = profileEditModel.Email;
        profile.Name = profileEditModel.Name;
        profile.PhotoUrl = profileEditModel.PhotoUrl;

        await _context.SaveChangesAsync();
        return profile.ToProfileModel();
    }
}
