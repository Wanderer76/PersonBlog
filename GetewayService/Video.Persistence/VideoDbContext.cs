using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Video.Domain.Entities;
using Video.Domain.Events;

namespace Video.Persistence
{
    public class VideoDbContext : BaseDbContext
    {
        public DbSet<VideoEvent> VideoEvents { get; set; }
        public DbSet<PostViewer> PostViewers { get; set; }


        public VideoDbContext(DbContextOptions<VideoDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Video");
        }
    }
}
