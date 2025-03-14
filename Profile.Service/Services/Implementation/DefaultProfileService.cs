using Blog.Domain.Entities;
using Blog.Service.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using System.Runtime.CompilerServices;
using Blog.Service.Models.Blog;
[assembly: InternalsVisibleTo("Profile.Test")]

namespace Blog.Service.Services.Implementation
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

            var blog = await _context.Get<PersonBlog>()
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

        public async Task<bool> CheckForViewAsync(Guid? userId, string? ipAddress)
        {
            if (userId == null && ipAddress == null)
            {
                return true;
            }
            return await _context.Get<PostViewer>()
                .Where(x => x.UserId == userId && x.UserIpAddress == ipAddress)
                .AnyAsync();
        }
    }
}
