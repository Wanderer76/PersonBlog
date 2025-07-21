using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Comments.Persistence
{
    public class CommentDbContext : BaseDbContext
    {

        public DbSet<Comment> Comments { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

        public CommentDbContext(DbContextOptions<CommentDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Comment");
            {
                var entity = modelBuilder.Entity<Comment>();
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Text).HasMaxLength(512);
                entity.Property(x => x.Text).IsRequired();
            }
            {
                var entity = modelBuilder.Entity<UserProfile>();
                entity.HasKey(x=>x.UserId);
                entity.Property(x=>x.PhotoUrl).IsRequired(false);
            }

        }
    }
}
