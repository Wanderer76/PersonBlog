using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Video.Persistence
{
    internal class VideoDbContextInitializer : IDbInitializer
    {
        private readonly VideoDbContext _db;

        public VideoDbContextInitializer(VideoDbContext db)
        {
            _db = db;
        }

        public void Initialize()
        {
            _db.Database.Migrate();
        }
    }
}
