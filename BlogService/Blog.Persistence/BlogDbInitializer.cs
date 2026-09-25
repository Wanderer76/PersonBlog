using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Blog.Persistence
{
    internal class BlogDbInitializer : IDbInitializer
    {
        private readonly BlogDbContext _context;

        public BlogDbInitializer(BlogDbContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            _context.Database.Migrate();
        }
    }
}
