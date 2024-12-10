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

                entity.HasData(new[]
                {
                    new AppProfile
                    {
                        Id = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d2e"),
                        UserId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                        Email = "ateplinsky@mail.ru",
                        ProfileState = ProfileState.Active,
                        FirstName = "Артём",
                        SurName="Теплинский",
                    }
                });
            }
            {
                var entity = modelBuilder.Entity<Blog>();
                entity.HasIndex(x => x.ProfileId).IsUnique();
                entity.HasData(new[]
               {
                    new Blog
                    {
                        Id = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d3e"),
                        Name = "Тест",
                        ProfileId =Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d2e"),
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                });

            }
            {
                var entity = modelBuilder.Entity<Subscriptions>();
                entity.HasIndex(x => new { x.ProfileId, x.BlogId }).IsUnique();
            }
        }
    }
}