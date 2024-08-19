using Microsoft.EntityFrameworkCore;

namespace Shared.Persistence;

public abstract class BaseDbContext : DbContext
{
    public BaseDbContext(DbContextOptions options)
        : base(options)
    {
    }
}