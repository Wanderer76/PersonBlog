using Blog.Domain.Entities;
using Blog.Persistence.Repository.Quries;
using Blog.Service.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultAdminService : IAdminService
    {
        private readonly IReadWriteRepository<IProfileEntity> _readWriteRepository;

        public DefaultAdminService(IReadWriteRepository<IProfileEntity> readWriteRepository)
        {
            _readWriteRepository = readWriteRepository;
        }

        public async Task<IEnumerable<ProfileModel>> GetAllProfilesAsync(int offset, int limit)
        {
            var profiles = await _readWriteRepository.GetAllProfilesPagedAsync(offset, limit);
            return profiles.Select(x => x.ToProfileModel()).ToList();
        }

        public async Task<ProfileModel> GetProfileByUserId(Guid userId)
        {
            var profile = await _readWriteRepository.Get<AppProfile>()
                .FirstAsync(x => x.UserId == userId);
            return profile.ToProfileModel();
        }

        //Todo реализовать в сервисе авторизации поиск ilike возвращающий массив userid
        public Task<IEnumerable<ProfileModel>> GetProfilesByLoginAsync(string login)
        {
            throw new NotImplementedException();
        }
    }
}
