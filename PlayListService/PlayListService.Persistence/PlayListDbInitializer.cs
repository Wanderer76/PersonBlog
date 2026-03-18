using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace PlayListService.Persistence;

internal class PlayListDbInitializer : IDbInitializer
{
    private readonly PlayListDbContext _dbContext;

    public PlayListDbInitializer(PlayListDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Initialize()
    {
        _dbContext.Database.Migrate();
    }
}
