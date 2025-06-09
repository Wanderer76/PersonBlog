using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using ViewReacting.Domain.Entities;

namespace VideoReacting.Persistence
{
    public class ViewReactingDbContext : BaseDbContext
    {
        public DbSet<UserPostView> UserPostViews { get; set; }
        public DbSet<ReactingEvent> ReactingEvents { get; set; }
        public DbSet<PostReaction> PostReactions { get; set; }
        public DbSet<SubscribedChanel> SubscribedChanels { get; set; }

        public ViewReactingDbContext(DbContextOptions<ViewReactingDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("VideoReacting");
        }
    }
}
