using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Persistence;

public class ProfileDbContext : BaseDbContext
{
    public DbSet<Domain.Entities.Profile> Profiles { get; set; }

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        {
            var entity = modelBuilder.Entity<Domain.Entities.Profile>();
            entity.HasIndex(x => x.UserId).IsUnique();
        }
    }
}