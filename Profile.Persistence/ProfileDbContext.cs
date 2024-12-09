using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Persistence;

public class ProfileDbContext : BaseDbContext
{
    public DbSet<AppProfile> Profiles { get; set; }
    public DbSet<Subscriptions> Subscriptions { get; set; }
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<VideoMetadata> VideoMetadata { get; set; }

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        {
            {
                var entity = modelBuilder.Entity<AppProfile>();
                entity.HasIndex(x => new { x.UserId, x.IsDeleted }).IsUnique();
            }
            {
                var entity = modelBuilder.Entity<Blog>();
                entity.HasIndex(x => x.ProfileId).IsUnique();
            }
            {
                var entity = modelBuilder.Entity<Subscriptions>();
                entity.HasIndex(x => new { x.ProfileId, x.BlogId }).IsUnique();
            }
        }
    }
}