using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Blog.Persistence
{
    internal class ProfileDbInitializer : IDbInitializer
    {
        private readonly BlogDbContext _context;

        public ProfileDbInitializer(BlogDbContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            _context.Database.Migrate();
        }
    }
}
