using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Runtime.CompilerServices;
using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Interfaces.Models.Profile;
[assembly: InternalsVisibleTo("Profile.Test")]

namespace Authentication.Service.Service.Implementation
{
    internal class DefaultProfileService : IProfileService
    {
        private readonly IReadWriteRepository<IAuthEntity> _context;

        public DefaultProfileService(IReadWriteRepository<IAuthEntity> profileRepository)
        {
            _context = profileRepository;
        }

        public async Task<ProfileModel> CreateProfileAsync(ProfileCreateModel profileCreateModel)
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
                firstName: profileCreateModel.FirstName ?? string.Empty,
                surName: profileCreateModel.SurName ?? string.Empty,
                lastName: profileCreateModel.LastName ?? string.Empty,
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
            profile.FirstName = profileEditModel.FirstName;
            profile.LastName = profileEditModel.LastName;
            profile.SurName = profileEditModel.SurName;
            profile.PhotoUrl = profileEditModel.PhotoUrl;

            await _context.SaveChangesAsync();

            return profile.ToProfileModel();
        }
    }
}
