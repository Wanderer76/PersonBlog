using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Persistence.Repository;
using Profile.Service.Models;
using Shared.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Interface.Implementation
{
    internal class DefaultAdminService : IAdminService
    {
        private readonly IReadWriteRepository<IProfileEntity> _readWriteRepository;
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
