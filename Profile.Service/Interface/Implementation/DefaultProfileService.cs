using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Models;
using Profile.Service.Models.Blog;
using Shared.Persistence;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Profile.Test")]

namespace Profile.Service.Interface.Implementation
{
    internal class DefaultProfileService : IProfileService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;

        public DefaultProfileService(IReadWriteRepository<IProfileEntity> profileRepository)
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

            var blog = await _context.Get<Blog>()
                .FirstOrDefaultAsync(x => x.ProfileId == profile.Id);

            return profile.ToProfileModel(blog?.ToBlogModel());
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
