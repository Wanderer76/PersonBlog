using Microsoft.EntityFrameworkCore;
using PlayListService.Domain.Entities;
using Shared.Persistence;

namespace PlayListService.Persistence;

public class PlayListDbContext : BaseDbContext
{
    public DbSet<PlayList> PlayLists { get; set; }

    public DbSet<PlayListItem> PlayListItems { get; set; }

    public PlayListDbContext(DbContextOptions<PlayListDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("PlayList");
        {
            var entity = modelBuilder.Entity<PlayListItem>();
            entity.HasKey(x => new { x.PlayListId, x.PostId });
        }
        {
            var entity = modelBuilder.Entity<PlayList>();
            entity.HasKey(x => x.Id);
        }
    }
}
