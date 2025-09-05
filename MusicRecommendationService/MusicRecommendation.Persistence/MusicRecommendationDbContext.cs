using Microsoft.EntityFrameworkCore;
using MusicRecommendation.Domain.Domain;
using Shared.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MusicRecommendation.Persistence;

public class MusicRecommendationDbContext : BaseDbContext
{
    public DbSet<Track> Tracks { get; set; }
    public DbSet<TrackGenre> TrackGenres { get; set; }
    public DbSet<UserListenHistory> UserListenHistory { get; set; }
    public DbSet<UserTrackAudition> UserTrackAuditions { get; set; }

    public MusicRecommendationDbContext(DbContextOptions<MusicRecommendationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("MusicRecommendation");
        {
            var entity = modelBuilder.Entity<UserTrackAudition>();
            entity.HasKey(x => new { x.UserId, x.TrackId });
        }
        {
            var entity = modelBuilder.Entity<Track>();
            //entity.Property(x => x.Genres)
            //    .HasColumnType("jsonb");
            //entity.Property(x => x.Genres)
            //    .HasConversion(input => JsonSerializer.Serialize(input,(JsonSerializerOptions)null),
            //                    output => JsonSerializer.Deserialize<List<Guid>>(output,(JsonSerializerOptions)null));
        }
        {
            var entity = modelBuilder.Entity<TrackGenre>();
            entity.HasKey(x => new { x.TrackId, x.Id });
        }

    }
}
