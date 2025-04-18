using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using ViewReacting.Domain.Entities;

namespace VideoReacting.Persistence
{
    public class ViewReactingDbContext : BaseDbContext
    {
        public DbSet<UserPostView> UserPostViews { get; set; }
        
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
