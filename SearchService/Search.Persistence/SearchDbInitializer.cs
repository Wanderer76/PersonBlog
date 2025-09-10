using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Search.Persistence
{
    internal class SearchDbInitializer : IDbInitializer
    {
        private readonly SearchDbContext _db;

        public SearchDbInitializer(SearchDbContext db)
        {
            _db = db;
        }

        public void Initialize()
        {
            _db.Database.Migrate();
        }
    }
}
