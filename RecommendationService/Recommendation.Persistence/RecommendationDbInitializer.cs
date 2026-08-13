using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Recommendation.Persistence;

internal sealed class RecommendationDbInitializer(RecommendationDbContext dbContext) : IDbInitializer
{
    public void Initialize() => dbContext.Database.Migrate();
}
