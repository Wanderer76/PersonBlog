using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Context = Shared.Persistence.IReadWriteRepository<Profile.Domain.Entities.IProfileEntity>;

namespace Profile.Persistence.Repository
{
    public static class ProfileQueryyExtension
    {
        public static async Task<IReadOnlyCollection<AppProfile>> GetAllProfilesPagedAsync(this Context context, int offset, int limit)
        {
            return await context.Get<AppProfile>()
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }
        public static async Task<AppProfile?> GetProfileByIdAsync(this Context context, Guid id)
        {
            return await context.Get<AppProfile>()
                .Where(x => x.IsDeleted == false)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
