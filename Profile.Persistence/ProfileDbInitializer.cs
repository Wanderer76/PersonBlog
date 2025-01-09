using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Profile.Persistence
{
    internal class ProfileDbInitializer : IDbInitializer
    {
        private readonly ProfileDbContext _context;

        public ProfileDbInitializer(ProfileDbContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            _context.Database.Migrate();
        }
    }
}
