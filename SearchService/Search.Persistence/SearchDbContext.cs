using Microsoft.EntityFrameworkCore;
using Search.Domain.Entities;
using Shared.Persistence;

namespace Search.Persistence
{
    public class SearchDbContext : BaseDbContext
    {
        public DbSet<WordScore> WordScores { get; set; }
        public DbSet<PostIndex> PostIndices { get; set; }
        public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Search");
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("Search", "pg_trgm");
            modelBuilder.HasPostgresExtension("Search", "btree_gin");

            {
                var entity = modelBuilder.Entity<PostIndex>();
                entity.HasKey(x => x.Id);
                entity.HasMany(x => x.Keywords)
                    .WithOne();
            }
            {
                var entity = modelBuilder.Entity<WordScore>();
                entity.HasKey(x => x.Id);
                entity.Property(x=>x.Id).ValueGeneratedOnAdd();
            }
        }
    }
}
