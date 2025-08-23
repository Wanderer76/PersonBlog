using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Shared.Persistence;

namespace Profile.Persistence
{
    public class ProfileDbContext : BaseDbContext
    {
        public DbSet<UserPostView> UserPostViews { get; set; }
        public DbSet<ReactingEvent> ReactingEvents { get; set; }
        public DbSet<PostReaction> PostReactions { get; set; }
        public DbSet<SubscribedChanel> SubscribedChanels { get; set; }
        public DbSet<AppProfile> Profiles { get; set; }
        public DbSet<PostBanRequest> PostBanRequests { get; set; }

        public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Profile");

            {
                var entity = modelBuilder.Entity<SubscribedChanel>();
                entity.HasIndex(x => new { x.UserId, x.BlogId }).IsUnique();
            }
            {
                var entity = modelBuilder.Entity<AppProfile>();
                entity.HasIndex(x => x.UserId).IsUnique();
                entity.Property(x => x.Id).ValueGeneratedOnAdd();
                entity.HasKey(x => x.Id);
            }
        }
    }
}
