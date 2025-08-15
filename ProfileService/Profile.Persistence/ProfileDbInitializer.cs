using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Profile.Persistence
{
    internal class ProfileDbInitializer : IDbInitializer
    {
        private readonly ProfileDbContext _dbContext;

        public ProfileDbInitializer(ProfileDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Initialize()
        {
            _dbContext.Database.Migrate();
        }
    }
}
