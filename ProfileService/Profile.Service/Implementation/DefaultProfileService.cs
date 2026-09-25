using Authentication.Contract.Events;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Application.Models.Profile;
using Profile.Application.Services;
using Profile.Domain.Entities;
using Shared.Persistence;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Profile.Test")]

namespace Profile.Service.Implementation;

internal class DefaultProfileService : IProfileService
{
    private readonly IReadWriteRepository<IUserEntity> _context;
    private readonly ICacheService _cacheService;
    private readonly IProfilePictureStore _profilePictureStore;
    public DefaultProfileService(IReadWriteRepository<IUserEntity> profileRepository, ICacheService cacheService, IProfilePictureStore profilePictureStore)
    {
        _context = profileRepository;
        _cacheService = cacheService;
        _profilePictureStore = profilePictureStore;
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
        return new ProfileModel
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Name = profile.Name,
            PhotoUrl = await _profilePictureStore.GetPictureUrlAsync(profile.Id),
            ProfileState = profile.ProfileState,
            CreatedAt = profile.CreatedAt,
        };
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
            return new ProfileModel
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Name = profile.Name,
                PhotoUrl = await _profilePictureStore.GetPictureUrlAsync(profile.Id),
                ProfileState = profile.ProfileState,
                CreatedAt = profile.CreatedAt,
            };
        }, 1);
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
        await using var transaction = await _context.BeginTransactionAsync();
        var profile = await _context.Get<AppProfile>()
            .FirstAsync(x => x.Id == profileEditModel.Id);
        _context.Attach(profile);
        profile.Name = profileEditModel.Name;
        if (profileEditModel.ProfilePicture != null)
        {
            await _profilePictureStore.UpdatePictureAsync(profile.Id, profileEditModel.ProfilePicture);
        }

        await _cacheService.RemoveCachedDataAsync(new AppProfileCacheKey(profile.UserId));
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new ProfileModel
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Name = profile.Name,
            PhotoUrl = await _profilePictureStore.GetPictureUrlAsync(profile.Id),
            ProfileState = profile.ProfileState,
            CreatedAt = profile.CreatedAt
        };
    }
}
