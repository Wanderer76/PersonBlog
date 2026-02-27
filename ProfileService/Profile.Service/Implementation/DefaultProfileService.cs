using Authentication.Contract.Events;
using Infrastructure.Services;
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
    private readonly ICacheService _cacheService;
    public DefaultProfileService(IReadWriteRepository<IUserEntity> profileRepository, ICacheService cacheService)
    {
        _context = profileRepository;
        _cacheService = cacheService;
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
        await _cacheService.RemoveCachedDataAsync(new AppProfileCacheKey(userId));
        await _context.SaveChangesAsync();
    }

    public async Task<ProfileModel> GetProfileByUserIdAsync(Guid userId)
    {
        return await _cacheService.GetOrAddDataAsync(new AppProfileCacheKey(userId), async () =>
        {
            var profile = await _context.Get<AppProfile>()
            .FirstAsync(x => x.UserId == userId);
            return profile.ToProfileModel();
        });
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
        profile.Name = profileEditModel.Name;
        profile.PhotoUrl = profileEditModel.PhotoUrl;
        await _cacheService.RemoveCachedDataAsync(new AppProfileCacheKey(profile.UserId));
        await _context.SaveChangesAsync();
        return profile.ToProfileModel();
    }
}
