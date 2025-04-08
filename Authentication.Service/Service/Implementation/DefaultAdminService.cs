using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Interfaces.Models.Profile;
using Authentication.Peristence.Repository;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Authentication.Service.Service.Implementation
{
    internal class DefaultAdminService : IAdminService
    {
        private readonly IReadWriteRepository<IAuthEntity> _readWriteRepository;

        public DefaultAdminService(IReadWriteRepository<IAuthEntity> readWriteRepository)
        {
            _readWriteRepository = readWriteRepository;
        }

        public async Task<IEnumerable<ProfileModel>> GetAllProfilesAsync(int offset, int limit)
        {
            var profiles = await _readWriteRepository.GetAllProfilesPagedAsync(offset, limit);
            return profiles.Select(x => x.ToProfileModel());
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
