using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace MusicRecommendation.Persistence
{
    internal class MusicDbInitializer : IDbInitializer
    {
        private readonly MusicRecommendationDbContext _db;

        public MusicDbInitializer(MusicRecommendationDbContext db)
        {
            _db = db;
        }

        public void Initialize()
        {
            _db.Database.Migrate();
        }
    }
}
