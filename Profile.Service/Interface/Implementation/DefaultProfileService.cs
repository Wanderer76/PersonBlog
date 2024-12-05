using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Models;
using Shared.Persistence;

namespace Profile.Service.Interface.Implementation
{
    internal class DefaultProfileService : IProfileService
    {
        private readonly IReadWriteRepository<IProfileEntity> _profileRepository;

        public DefaultProfileService(IReadWriteRepository<IProfileEntity> profileRepository)
        {
            _profileRepository = profileRepository;
        }

        public async Task<ProfileModel> CreateProfileAsync(ProfileCreateModel profileCreateModel)
        {
            var isProfileAlreadyExist = await _profileRepository.Get<AppProfile>()
                .AnyAsync(x => x.UserId == profileCreateModel.UserId);
            if (isProfileAlreadyExist)
            {
                throw new ArgumentException("Пользователь уже существует");
            }

            var profile = new AppProfile
            {
                Id = Guid.NewGuid(),
                Birthdate = profileCreateModel.Birthdate,
                Email = profileCreateModel.Email,
                FirstName = profileCreateModel.FirstName,
                SurName = profileCreateModel.SurName,
                LastName = profileCreateModel.LastName,
                ProfileState = ProfileState.Active,
                UserId = profileCreateModel.UserId,
                IsDeleted = false
            };
            _profileRepository.Add(profile);
            await _profileRepository.SaveChangesAsync();
            return profile.ToProfileModel();
        }

        public async Task DeleteProfileByUserIdAsync(Guid userId)
        {
            var profile = await _profileRepository.Get<AppProfile>()
                .FirstAsync(x => x.UserId == userId);

            _profileRepository.Attach(profile);
            profile.IsDeleted = true;
            await _profileRepository.SaveChangesAsync();
        }

        public async Task<ProfileModel> GetProfileByUserIdAsync(Guid userId)
        {
            var profile = await _profileRepository.Get<AppProfile>()
                .FirstAsync(x => x.UserId == userId);

            return profile.ToProfileModel();
        }

        public Task SubscribeToBlog(Guid blogId)
        {
            throw new NotImplementedException();
        }

        public Task UnSubscribeFromBlog(Guid blogId)
        {
            throw new NotImplementedException();
        }

        public async Task<ProfileModel> UpdateProfileAsync(ProfileUpdateModel profileEditModel)
        {
            var profile = await _profileRepository.Get<AppProfile>()
            .FirstAsync(x => x.Id == profileEditModel.Id);

            _profileRepository.Attach(profile);
            profile.Birthdate = profileEditModel.Birthdate;
            profile.Email = profileEditModel.Email;
            profile.FirstName = profileEditModel.FirstName;
            profile.LastName = profileEditModel.LastName;
            profile.SurName = profileEditModel.SurName;
            profile.PhotoUrl = profileEditModel.PhotoUrl;

            await _profileRepository.SaveChangesAsync();

            return profile.ToProfileModel();
        }
    }
}
