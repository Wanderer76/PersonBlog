using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Persistence;

public class ProfileDbContext : BaseDbContext
{
    public DbSet<AppProfile> Profiles { get; set; }
    public DbSet<Subscriptions> Subscriptions { get; set; }
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        {
            var entity = modelBuilder.Entity<AppProfile>();
            entity.HasIndex(x => x.UserId).IsUnique();
        }
    }
}