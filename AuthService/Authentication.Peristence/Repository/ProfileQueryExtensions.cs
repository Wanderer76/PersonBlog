using Authentication.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using ReadContext = Shared.Persistence.IReadRepository<Authentication.Domain.Entities.IAuthEntity>;


namespace Authentication.Peristence.Repository
{
    public static class ProfileQueryExtensions
    {
        public static async Task<IReadOnlyCollection<AppProfile>> GetAllProfilesPagedAsync(this ReadContext context, int offset, int limit)
        {
            return await context.Get<AppProfile>()
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
        }
        public static async Task<AppProfile?> GetProfileByIdAsync(this ReadContext context, Guid id)
        {
            return await context.Get<AppProfile>()
            .Where(x => x.IsDeleted == false)
            .FirstOrDefaultAsync(x => x.Id == id);
        }

        public static async Task<AppProfile?> GetProfileByUserIdAsync(this ReadContext context, ICacheService cache, Guid userId)
        {
            var profile = await cache.GetCachedDataAsync<AppProfile>($"{nameof(AppProfile)}:{userId}");
            if (profile == null)
            {
                profile = await context.Get<AppProfile>()
                    .Where(x => x.IsDeleted == false)
                    .Where(x => x.UserId == userId)
                    .FirstOrDefaultAsync();

                if (profile != null)
                    await cache.SetCachedDataAsync($"{nameof(AppProfile)}:{userId}", profile, TimeSpan.FromMinutes(10));
            }
            return profile;
        }
    }
}
