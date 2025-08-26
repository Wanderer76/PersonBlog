using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Music.Persistence
{
    internal class MusicDbInitializer : IDbInitializer
    {
        private readonly MusicDbContext _db;

        public MusicDbInitializer(MusicDbContext db)
        {
            _db = db;
        }

        public void Initialize()
        {
            _db.Database.Migrate();
        }
    }
}
